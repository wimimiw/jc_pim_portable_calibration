using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace PimCalibration
{
    /// <summary>
    ///信号发生器 
    /// </summary>
    class SignalGenerator
    {
        private ManualResetEvent _serialReceiveMre;
        private SerialPort _serialPort;
        private string _readLine = string.Empty;
        public static readonly float Error = 0.1f;

        /// <summary>
        /// 信号发生器初始化
        /// </summary>
        /// <param name="comName"></param>
        /// <param name="baudRate"></param>
        public SignalGenerator(string comName, int baudRate)
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
            _readLine = _serialPort.ReadLine();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        private bool Communit(string str, bool wait)
        {
            _readLine = string.Empty;
            _serialPort.WriteLine(str);

            if (wait)
            {
                bool result = _serialReceiveMre.WaitOne(1000);
                _serialReceiveMre.Reset();
                return result;
            }
            else
                return true;
        }
        /// <summary>
        /// 复位
        /// </summary>
        /// <returns></returns>
        public bool Preset()
        {          
            Communit(":SYST:COMM:SER:ECHO OFF", false);
            Communit(":SYST:PRES", false);
            Thread.Sleep(1000);
            Communit(":SYST:COMM:SER:RES", false);
            return true;
        }
        /// <summary>
        /// 设置功率
        /// </summary>
        /// <param name="power"></param>
        /// <param name="freq"></param>
        /// <returns></returns>
        public void WritePower(float power)
        {
            Communit(":POW:AMPL " + power.ToString() + " dBm", false); 
        }
        /// <summary>
        /// 设置频率
        /// </summary>
        /// <param name="freq"></param>
        public void WriteFreq(float freq)
        {
            Communit(":FREQ " + freq.ToString() + " MHz", false);          
        }
        /// <summary>
        /// 读功率值
        /// </summary>
        /// <returns></returns>
        public float ReadPower()
        {
            Communit(":POW:AMPL?" , true);

            //if (_readLine == string.Empty)
            //    return SignalGenerator.Error;
            //else
            //    return float.Parse(_readLine);

            try
            {
                return float.Parse(_readLine.Trim());
            }
            catch (Exception ex)
            {
                return SignalGenerator.Error;
            }
        }
        /// <summary>
        /// 读频率值
        /// </summary>
        /// <returns></returns>
        public float ReadFreq()
        {
            Communit(":FREQ?", true);
            try
            {
                return float.Parse(_readLine.Trim());
            }
            catch (Exception ex)
            {
                return SignalGenerator.Error;
            }
        }
        /// <summary>
        /// 打开信号源
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            Communit(":OUTP:STAT ON", false);
            return true;
        }
        /// <summary>
        /// 关闭信号源
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            Communit(":OUTP:STAT OFF", false);
            return true;
        }
    }
}
