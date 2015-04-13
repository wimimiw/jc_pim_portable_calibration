using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace PimCalibration
{
    /// <summary>
    /// 功率检测类
    /// </summary>
    class PowerMeter
    {
        private ManualResetEvent _serialReceiveMre;
        private SerialPort _serialPort;
        private string _readLine = string.Empty,_writeLine = string.Empty;
        public const float READ_ERROR = 100f;

        /// <summary>
        /// 功率计初始化
        /// </summary>
        /// <param name="comName"></param>
        /// <param name="baudRate"></param>
        public PowerMeter(string comName,int baudRate)
        {
            _serialPort = new SerialPort(  comName,
                                                            baudRate,
                                                            Parity.None,
                                                            8,
                                                            StopBits.One);

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            _serialPort.Open();

            _serialReceiveMre = new ManualResetEvent(false);
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _readLine +=  _serialPort.ReadLine();
            _serialReceiveMre.Set();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }

        public string GetComInfo()
        {
            return _serialPort.ToString();
        }

        private bool Communit(string str,bool wait)
        {
            _writeLine = str;
            _readLine = string.Empty;

            _serialPort.WriteLine(str);

            if (wait)
            {
                bool result =  _serialReceiveMre.WaitOne(500);
                _serialReceiveMre.Reset();
                return result;
            }
            else
                return true;
        }

        public bool GetError()
        {
            Communit("SYST:ERR?", true);
            return false;
        }

        /// <summary>
        /// 功率计测试准备
        /// </summary>
        /// <returns></returns>
        public bool Preset()
        {
            //关闭回显
            Communit("SYST:COMM:SER:TRAN:ECHO OFF", false);
            Thread.Sleep(100);
            Communit("SYST:PRES", false);
            Thread.Sleep(500);
            Communit("INIT1:CONT ON", false);
            Thread.Sleep(500);
            //Communit("UNIT:POW DBM", false);
            //Set the power meter for channel offsets of -10  dB
            //Communit("SENS1:CORR:GAIN1 30", false);
            //This command enters a display offset of 20 dB to the lower window.
            //Communit("CALC1:GAIN 30", false);
            return true;
        }

        /// <summary>
        /// 读取设置频点相应的的功率值，不成功返回PowerMeter.READ_ERROR
        /// </summary>
        /// <param name="freq"></param>
        /// <returns></returns>
        public float Read(float freq)
        {
            //读取功率值
            if (Communit("FETC2:POW:AC?", true) == false) 
                return (float)PowerMeter.READ_ERROR;

            float result = 0;

            try
            {
                return result = float.Parse(_readLine);
            }
            catch(Exception ex)
            {
                return PowerMeter.READ_ERROR;
            }

            //if (float.IsNaN(result))
            //{
            //    return PowerMeter.READ_ERROR;
            //}
            //else
            //{ 
            //    return result;
            //}
        }
    }
}
