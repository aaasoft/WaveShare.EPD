using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveShare.EPD
{
    public abstract class EPD_Base : IDisposable
    {
        // Pin definition
        public const int RST_PIN = 17;
        public const int DC_PIN = 25;
        public const int CS_PIN = 8;
        public const int BUSY_PIN = 24;

        private GpioController? gpio;
        private SpiDevice? spi;

        public abstract int Width { get; }
        public abstract int Height { get; }

        public EPD_Base(PinNumberingScheme numberingScheme, GpioDriver driver)
        {
            gpio = new GpioController(numberingScheme, driver);
            gpio.OpenPin(RST_PIN, PinMode.Output);
            gpio.OpenPin(DC_PIN, PinMode.Output);
            gpio.OpenPin(CS_PIN, PinMode.Output);
            gpio.OpenPin(BUSY_PIN, PinMode.Input);

            spi = SpiDevice.Create(new SpiConnectionSettings(0, 0)
            {
                Mode = SpiMode.Mode0,
                ClockFrequency = 4000000
            });
        }
        
        public abstract void Init();
        public abstract void Clear(byte color);

        protected void digital_write(int pin, PinValue value)
        {
            if (gpio == null)
                throw new IOException("gpio is null.");
            gpio.Write(pin, value);
        }

        protected PinValue digital_read(int pin)
        {
            if (gpio == null)
                throw new IOException("gpio is null.");
            return gpio.Read(pin);
        }

        protected void spi_writebyte(byte data)
        {
            if (spi == null)
                throw new IOException("spi is null.");
            spi.WriteByte(data);
        }

        protected void spi_writebytes(byte[] data)
        {
            if (spi == null)
                throw new IOException("spi is null.");
            spi.Write(data);
        }

        protected void delay_ms(int time)
        {
            Thread.Sleep(time);
        }

        public virtual void Dispose()
        {
            if (spi != null)
            {
                spi.Dispose();
                spi = null;
            }
            if (gpio != null)
            {
                gpio.Write(RST_PIN, 0);
                gpio.Write(DC_PIN, 0);
                gpio.ClosePin(RST_PIN);
                gpio.ClosePin(DC_PIN);
                gpio.ClosePin(CS_PIN);
                gpio.ClosePin(BUSY_PIN);
                gpio.Dispose();
                gpio = null;
            }
        }
    }
}
