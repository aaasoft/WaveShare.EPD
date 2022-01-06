using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Device.Gpio;

namespace WaveShare.EPD
{
    public class EPD_2in9 : EPD_Base
    {
        public const int EPD_WIDTH = 128;
        public const int EPD_HEIGHT = 296;

        public byte[] lut_full_update = {
            0x50, 0xAA, 0x55, 0xAA, 0x11, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xFF, 0xFF, 0x1F, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public byte[] lut_partial_update = {
            0x10, 0x18, 0x18, 0x08, 0x18, 0x18,
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x13, 0x14, 0x44, 0x12,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public override int Width => EPD_WIDTH;
        public override int Height => EPD_HEIGHT;

        public EPD_2in9(PinNumberingScheme numberingScheme, GpioDriver driver) : base(numberingScheme, driver)
        {
        }

        public void Reset()
        {
            digital_write(RST_PIN, 1);
            delay_ms(200);
            digital_write(RST_PIN, 0);
            delay_ms(5);
            digital_write(RST_PIN, 1);
            delay_ms(200);
        }

        public void SendCommand(byte command)
        {
            digital_write(DC_PIN, 0);
            digital_write(CS_PIN, 0);
            spi_writebyte(command);
            digital_write(CS_PIN, 1);
        }

        public void SendData(byte data)
        {
            digital_write(DC_PIN, 1);
            digital_write(CS_PIN, 0);
            spi_writebyte(data);
            digital_write(CS_PIN, 1);
        }

        public void ReadBusy()
        {
            while (digital_read(BUSY_PIN) == 1)      //  0: idle, 1: busy
                delay_ms(200);
        }

        public void TurnOnDisplay()
        {
            SendCommand(0x22); // DISPLAY_UPDATE_CONTROL_2
            SendData(0xC4);
            SendCommand(0x20); // MASTER_ACTIVATION
            SendCommand(0xFF); // TERMINATE_FRAME_READ_WRITE
            ReadBusy();
        }

        public void SetWindow(int x_start, int y_start, int x_end, int y_end)
        {
            SendCommand(0x44); //# SET_RAM_X_ADDRESS_START_END_POSITION
            // x point must be the multiple of 8 or the last 3 bits will be ignored
            SendData((byte)((x_start >> 3) & 0xFF));
            SendData((byte)((x_end >> 3) & 0xFF));
            SendCommand(0x45); // SET_RAM_Y_ADDRESS_START_END_POSITION
            SendData((byte)(y_start & 0xFF));
            SendData((byte)((y_start >> 8) & 0xFF));
            SendData((byte)(y_end & 0xFF));
            SendData((byte)((y_end >> 8) & 0xFF));
        }

        public void SetCursor(int x, int y)
        {
            SendCommand(0x4E); // SET_RAM_X_ADDRESS_COUNTER
                               // x point must be the multiple of 8 or the last 3 bits will be ignored
            SendData((byte)((x >> 3) & 0xFF));
            SendCommand(0x4F); // SET_RAM_Y_ADDRESS_COUNTER
            SendData((byte)(y & 0xFF));
            SendData((byte)((y >> 8) & 0xFF));
            ReadBusy();
        }

        public override void Init()
        {
            init(lut_full_update);
        }

        private void init(byte[] lut)
        {
            // EPD hardware init start
            Reset();

            SendCommand(0x01); // DRIVER_OUTPUT_CONTROL
            SendData((EPD_HEIGHT - 1) & 0xFF);
            SendData(((EPD_HEIGHT - 1) >> 8) & 0xFF);
            SendData(0x00); // GD = 0 SM = 0 TB = 0

            SendCommand(0x0C); // BOOSTER_SOFT_START_CONTROL 
            SendData(0xD7);
            SendData(0xD6);
            SendData(0x9D);


            SendCommand(0x2C); // WRITE_VCOM_REGISTER
            SendData(0xA8); // VCOM 7C

            SendCommand(0x3A); // SET_DUMMY_LINE_PERIOD
            SendData(0x1A); // 4 dummy lines per gate

            SendCommand(0x3B); // SET_GATE_TIME
            SendData(0x08); // 2us per line

            SendCommand(0x11); // DATA_ENTRY_MODE_SETTING
            SendData(0x03); // X increment Y increment

            SendCommand(0x32);// WRITE_LUT_REGISTER

            for (var i = 0; i < lut.Length; i++)
                SendData(lut[i]);
            // EPD hardware init end
        }

        protected byte[] Getbuffer(Image<Rgba32> image)
        {
            var buf = new byte[Width / 8 * Height];
            Array.Fill(buf, (byte)0xFF);

            image.Mutate(x => x.BlackWhite());

            var imwidth = image.Width;
            var imheight = image.Height;


            if (imwidth == Width && imheight == Height)
            {
                for (var y = 0; y < imheight; y++)
                {
                    for (var x = 0; x < imwidth; x++)
                    {
                        var pixel = image[x, y];
                        if (pixel.Rgba == 0)
                        {
                            var b = 0x80 >> (x % 8);
                            b = ~b;
                            buf[(x + y * Width) / 8] &= (byte)b;
                        }
                    }
                }
            }
            else if (imwidth == Height && imheight == Width)
            {
                for (var y = 0; y < imheight; y++)
                {
                    for (var x = 0; x < imwidth; x++)
                    {
                        var newx = y;
                        var newy = Height - x - 1;
                        var pixel = image[x, y];
                        if (pixel.Rgba != 0)
                        {
                            var b = 0x80 >> (y % 8);
                            b = ~b;
                            buf[(newx + newy * Width) / 8] &= (byte)b;
                        }
                    }
                }
            }
            return buf;
        }

        public override void Display(Image<Rgba32> image)
        {
            Display(Getbuffer(image));
        }

        protected void Display(byte[] image)
        {
            if (image == null)
                return;
            SetWindow(0, 0, Width - 1, Height - 1);
            for (var j = 0; j < Height; j++)
            {
                SetCursor(0, j);
                SendCommand(0x24); // WRITE_RAM
                for (var i = 0; i < Width / 8; i++)
                    SendData(image[i + j * Width / 8]);
            }
            TurnOnDisplay();
        }

        public override void Clear(byte color)
        {
            SetWindow(0, 0, Width - 1, Height - 1);
            for (var j = 0; j < Height; j++)
            {
                SetCursor(0, j);
                SendCommand(0x24); // WRITE_RAM
                for (var i = 0; i < Width / 8; i++)
                    SendData(color);
            }
            TurnOnDisplay();
        }

        public override void Dispose()
        {
            SendCommand(0x10); // DEEP_SLEEP_MODE
            SendData(0x01);

            delay_ms(2000);
            base.Dispose();
        }
    }
}