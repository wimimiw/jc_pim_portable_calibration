using System;
using System.Collections.Generic;
using System.Text;
using SpectrumLib.Defines;
using SpectrumLib.Models;
using SpectrumLib.Spectrums;
using System.Threading;
using System.Drawing;

namespace PimCalibration
{
    class Spectrum
    {
        ///// <summary>
        ///// 频谱仪类型
        ///// </summary>
        //private ESpectrumType SpectrumType;
        /// <summary>
        /// 频谱分析对象
        /// </summary>
        private SpectrumLib.Models.ScanModel _scanModel;
        /// <summary>
        /// 频谱仪接口对象
        /// </summary>
        private SpectrumLib.ISpectrum ISpectrumObj;
        /// <summary>
        /// 频谱分析线程
        /// </summary>
        private Thread thdAnalysis;
        /// <summary>
        /// 频谱分析参数结构
        /// </summary>
        private struct ScanParamObj
        {
            /// <summary>
            /// 扫描起始频率(KHz)
            /// </summary>
            public int StartAnalysisFreq;
            /// <summary>
            /// 扫描结束频率(KHz)
            /// </summary>
            public int EndAnalysisFreq;
            /// <summary>
            /// 扫描中心频率(KHz)
            /// </summary>
            public int CenterAnalysisFreq;
            /// <summary>
            /// 扫描带宽
            /// </summary>
            public int AnalysisSpan;
            /// <summary>
            /// ATT衰减
            /// </summary>
            public int AnalysisAtt;
        }

        //delegate void Del_SpecRun(int startFreq, int stopFreq, int att, int rbw, int vbw, int span);

        public Spectrum(int type,IntPtr wndHandle)
        {
            ISpectrumObj = new Deli(wndHandle, MessageID.SPECTRUEME_SUCCED, MessageID.SPECTRUM_ERROR);            
        }

        public bool IsConnected()
        {
            return ISpectrumObj.IsConnected();
        }

        public void Connecting()
        {
            //return (ISpectrumObj.ConnectSpectrum())==0 ? false : true;
            try
            {
                ISpectrumObj.ConnectSpectrum();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region 开始进行频谱分析

        public void SetRBW(int value)
        {
            ISpectrumObj.SetRBW(value);
        }

        public void SetVBW(int value)
        {
            //ISpectrumObj.setvb
        }

        /// <summary>
        /// 开始进行频谱分析
        /// </summary>
        /// <param name="startFreq">扫描起始频率(KHz)</param>
        /// <param name="stopFreq">扫描结束频率(KHz)</param>
        /// <param name="att">ATT衰减</param>
        /// <param name="rbw"></param>
        /// <param name="vbw"></param>
        public void StartAnalysis(int startFreq,int stopFreq,int att,int rbw,int vbw,int span)
        {
            _scanModel = new ScanModel();
            _scanModel.StartFreq = startFreq;//起始频率
            _scanModel.Unit = CommonDef.EFreqUnit.KHz;//单位
            _scanModel.EndFreq = stopFreq;//结束频率
            _scanModel.Att = att;
            _scanModel.Rbw = rbw;
            _scanModel.Vbw = vbw;
            _scanModel.Continued = true;
            _scanModel.FullPoints = true;
            _scanModel.TimeSpan = span;
            _scanModel.TimeDelay = 500;//只在pim配置文件里面设置
            //_scanModel.OffsetIndex = (int)numericUpDownOffset.Value;
            _scanModel.ProtectNEC = false;
            _scanModel.EnableTimer = true;
            _scanModel.MaxP = -40;
            _scanModel.Deli_averagecount = 5;
            // ScanModel.Deli_detector = "AVERage";
            _scanModel.Deli_ref = -50;
            //ScanModel.Deli_startspe = 1;
            _scanModel.DeliSpe = CommonDef.SpectrumType.Deli_SPECTRUM;
            _scanModel.Deli_isSpectrum = true;
            _scanModel.Deli_setChannelPower = false;

            thdAnalysis = new Thread(ISpectrumObj.StartAnalysis);
            thdAnalysis.IsBackground = true;
            thdAnalysis.Start(_scanModel as object);
        }

        #endregion    
 
        #region 停止当前频谱分析
        /// <summary>
        /// 停止当前频谱分析
        /// </summary>
        public void StopAnalysis()
        {
            ISpectrumObj.StopAnalysis();
            if (thdAnalysis.IsAlive)
            {
                thdAnalysis.Abort();
            }

            thdAnalysis = null;
        }
        #endregion

        #region 
        /// <summary>
        /// 窗体收到频谱分析执行成功的消息后，调用该函数，将其告之循环
        /// </summary>
        internal float FindMaxValue()
        {
            int intmax = 0;
            PointF[] values;
            float dBmValue = float.MinValue;

            //获取频谱分析数据
            values = (PointF[])ISpectrumObj.GetSpectrumData();

            if (values == null) return 0;

            //在取得的频谱分析数据中，搜索Y值最大点，将其Y值作为收信值
            for (int J = 0; J < values.Length; J++)
            {
                if (values[J].Y > dBmValue)
                {
                    intmax = J;
                    dBmValue = values[J].Y;
                }
            }

            return dBmValue;
        }

        #endregion

        public void Dispose()
        {
            ISpectrumObj = null;
        }
    }
}
