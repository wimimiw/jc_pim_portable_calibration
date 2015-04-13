using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SpectrumLib;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PimCalibration
{
    public partial class MainForm : Form
    {
        #region 变量
        private ManualResetEvent __handleSpecNormal = new ManualResetEvent(false);
        private ManualResetEvent __handleSpecError = new ManualResetEvent(false);
        private ManualResetEvent __handlePARev = new ManualResetEvent(false);        
        //private ManualResetEvent handleThrdAbort = new ManualResetEvent(false);
        private bool __txStartValid = false;
        //private bool rxStartValid = false;
        private int __timeCnt = 0;
        private Spectrum __specObj = null;        
        private const string __NoACK = "==>PA NO ACK!";
        private bool __bTxCalRun = false;
        private bool __bRxCalRun = false;
        private object __threadLock = new object();

        private enum ButtonSwitchStatus
        {
            Ready,
            Checking,
            CheckPass,
            CheckFailed,
            Calibrating,
            Calibrated,
        }
        #endregion

        #region 初始化
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.listBox1.DrawMode = DrawMode.OwnerDrawFixed;

            try
            {
                ParameterManage.LoadTxChannelPara(Application.StartupPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                ParameterManage.LoadRxChannelPara(Application.StartupPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("rx_calib.ini has error");
            }

            try
            {
                ParameterManage.LoadConfiguration(Application.StartupPath);
            }
            catch(Exception ex)
            {
                MessageBox.Show("configuration.ini has error");
            }

            if (ParameterManage.tx.SampleOnly)
                ParameterManage.LoadTxCalibPara(Application.StartupPath);
            //ParameterManage.SaveTxChannelPara(Application.StartupPath);
            //ParameterManage.SaveRxChannelPara(Application.StartupPath);

            this.Text = Assembly.GetExecutingAssembly().GetName().Name + "  " + Assembly.GetEntryAssembly().GetName().Version;
            this.tbDisplay.Text = ParameterManage.txInfo;
            
            this.rbTX.Select();
            this.toolStripStatusLabel1.Text = string.Empty;
            this.toolStripStatusLabel2.Text = string.Empty;

            int freq = ParameterManage.rx.channel[2].stop;

            ButtonSwitch(ButtonSwitchStatus.Ready);
            ParameterManage.tx.RFPriority = RFPriority.LvlTwo;
            //ButtonSwitch(ButtonSwitchStatus.CheckPass);
        }

        #endregion

        #region 捕获窗口线程消息
        /// <summary>
        /// 捕获窗口线程消息
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == MessageID.SPECTRUEME_SUCCED)
            {
                __handleSpecNormal.Set();                
            }
            if (m.Msg == MessageID.SPECTRUM_ERROR)
            {
                __handleSpecError.Set();                
            }
            if (m.Msg == MessageID.RF_SUCCED_ALL)
            {
                if (m.WParam.ToInt32() == ParameterManage.tx.PA1Addr)
                {
                    __handlePARev.Set();
                }
                if (m.WParam.ToInt32() == ParameterManage.tx.PA2Addr)
                {
                    __handlePARev.Set();
                }
            }
            else if (m.Msg == MessageID.RF_ERROR)
            {
                if (m.WParam.ToInt32() == ParameterManage.tx.PA1Addr)
                {
                    __handlePARev.Set();
                }

                if (m.WParam.ToInt32() == ParameterManage.tx.PA2Addr)
                {
                    __handlePARev.Set();
                }
            }
            else
                base.WndProc(ref m);
        }
        #endregion        

        #region 功放校准

        #region 发信校准
        /// <summary>
        /// 中断校准
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="pm"></param>
        private void TxCaliStopDuring(int addr,PowerMeter pm)
        {
            //ParameterManage.SaveTxChannelPara(Application.StartupPath);      
            //ParameterManage.SaveTxFailedData(Application.StartupPath);
            MessageBox.Show(this,"中断退出！注意，不保存任何数据！","",MessageBoxButtons.OK,MessageBoxIcon.Stop);

            int Lvl = ParameterManage.tx.RFPriority;
            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(4000))
            {
                MessageBox.Show("无法关闭功放，请使用OMT关闭功放，串口号"+addr.ToString(),"",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }

            RFSignal.RFFinalize();

            pm.Dispose();

            PrintListInfo("==>Calibration is Stop!");
            PrintListInfo("Exit...");
            ButtonSwitch(ButtonSwitchStatus.Calibrated);

            this.BeginInvoke(new MethodInvoker(delegate { this.Close(); }));
        }
        /// <summary>功放通道处理函数
        /// 功放通道处理函数
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="pm"></param>
        private bool TxCalibChannel(int addr,PowerMeter pm,TxCalDataStruct.EPowerDivide ed)
        {
            int dispNum = addr - Math.Min(ParameterManage.tx.PA1Addr, ParameterManage.tx.PA2Addr) + 1;
            PrintListInfo("==>Start Calibrate [PA" + dispNum.ToString()+"]...");

            int Lvl = ParameterManage.tx.RFPriority;

            PACalPara paCalPara = null;

            if (addr == ParameterManage.tx.PA1Addr)
            {
                paCalPara = ParameterManage.tx.PA[0];
            }
            else if (addr == ParameterManage.tx.PA2Addr)
            {
                paCalPara = ParameterManage.tx.PA[1];
            }

            //PowerStatus status = new PowerStatus();
            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFSample2(addr, Lvl);
            RFSignal.RFPower(addr, Lvl, paCalPara.power[0]);
            RFSignal.RFFreq(addr, Lvl, paCalPara.freq[0]);
            RFSignal.RFOpenSource(Lvl);
            RFSignal.RFOn(addr, Lvl);            
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(4000))
            {
                PrintListInfo(__NoACK);
                goto TX_CALIB_ERROR;
            }
            __handlePARev.Reset();

            Thread.Sleep(300);

            //找到功率分界点序号
            int pwrIdxStart = 0;
            int pwrIdxEnd = 0;
            int pwrIdxDiv = 0;

            for (int i = 0; i < paCalPara.power.Count; i++)
            {
                if (paCalPara.power[i] > ParameterManage.tx.PowerOffsetSwitch)
                {
                    pwrIdxDiv = i;
                    break;
                }
            }

            pwrIdxStart = (ed == TxCalDataStruct.EPowerDivide.low) ? 0 : pwrIdxDiv;
            pwrIdxEnd = (ed == TxCalDataStruct.EPowerDivide.low) ? pwrIdxDiv : paCalPara.power.Count;

            for (int i = 0; i < paCalPara.freq.Count; i++)
            {
                for (int j = pwrIdxStart; j < pwrIdxEnd; j++)
                {                                           
                    int cycleCnt = 0;
                    int calibLarge = 2;
                    float power = paCalPara.power[j];
                    float powerRead = power;
                    float freq = paCalPara.freq[i];
                    bool b_calibOK = true;

                    RFSignal.RFClear(addr, Lvl);
                    RFSignal.RFSetAtt(addr, Lvl, (int)(paCalPara.powerAtt[j] * 2));
                    RFSignal.RFStart(addr);

                    if (!__handlePARev.WaitOne(2000))
                    {
                        PrintListInfo(__NoACK);
                        goto TX_CALIB_ERROR;
                    }
                    __handlePARev.Reset();

                    do
                    {
                        if (!this.__bTxCalRun)
                        {
                            PrintListInfo("==>捕捉指令，等待退出...");
                            TxCaliStopDuring(addr, pm);
                            Thread.CurrentThread.Abort();
                        }

                        if (calibLarge-- > 0)
                        {
                            if (Math.Abs(powerRead - power) < 7f)//超过该值，系统的故障
                                power = power * 2 - powerRead;
                        }
                        else
                            power += ParameterManage.tx.Step * (paCalPara.power[j] - powerRead) / Math.Abs(paCalPara.power[j] - powerRead);

                        RFSignal.RFClear(addr, Lvl);
                        RFSignal.RFFreq(addr, Lvl, freq);    
                        RFSignal.RFPower(addr, Lvl, power);                                                                               
                        RFSignal.RFStart(addr);

                        if (!__handlePARev.WaitOne(4000))
                        {
                            PrintListInfo(__NoACK);
                            goto TX_CALIB_ERROR;
                        }
                        __handlePARev.Reset();

                        Thread.Sleep(ParameterManage.tx.CalDelay);

                        PrintListInfo("==>[PA"+dispNum.ToString()+"]:"+
                                            " Freq = " + paCalPara.freq[i].ToString("F0") + " MHz" +
                                            " SetPower = " + power.ToString("F2") + "dBm Count:" + cycleCnt.ToString());
                        
                        //读取功率计
                        powerRead = pm.Read(freq);
                        if (powerRead == PowerMeter.READ_ERROR)
                        {
                            PrintListInfo("==>Read Power Failed!");
                            goto TX_CALIB_ERROR;
                        }                        

                        //补偿耦合器值
                        powerRead += (ed == TxCalDataStruct.EPowerDivide.low) ? ParameterManage.tx.PowerOffsetLow : ParameterManage.tx.PowerOffsetHigh;

                        PrintListInfo( "==>[PA" + dispNum.ToString() + "]:" +
                                              " Freq = " + paCalPara.freq[i].ToString("F0") + " MHz" +
                                              " ReadPower = " + powerRead.ToString("F2") + " dBm ");

                        if (cycleCnt++ > ParameterManage.tx.CycleCnt)
                        {
                            b_calibOK = false;                              
                            power = paCalPara.power[j];

                            if (addr == ParameterManage.tx.PA1Addr)
                                ParameterManage.tx.errCollect[0, i, j] = true;
                            else if (addr == ParameterManage.tx.PA2Addr)
                                ParameterManage.tx.errCollect[1, i, j] = true;  
                     
                            break;
                        }

                    } while (Math.Abs(paCalPara.power[j]-powerRead) > paCalPara.powerFloat[j]);                    
 
                    if (addr == ParameterManage.tx.PA1Addr)
                    {
                        ParameterManage.tx.powerCalib[0, i, j] = power - paCalPara.power[j];                        
                    }
                    else if (addr == ParameterManage.tx.PA2Addr)
                    {
                        ParameterManage.tx.powerCalib[1, i, j] = power - paCalPara.power[j];
                    }

                    if (b_calibOK)
                    {
                        PrintListInfo("==>[PA" + dispNum.ToString() + "] " +
                                            "Freq = " + paCalPara.freq[i] + " MHz " +
                                            "Calibrate Success!");
                    }
                    else
                    {
                        PrintListInfo("==>[PA" + dispNum.ToString() + "] " +
                                            "Freq = " + paCalPara.freq[i] + " MHz " +
                                            "Calibrate Failed!");
                    }

                    PrintListInfo("---------------------------------------------------------------");
                }
            }

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(2000))
            {
                PrintListInfo(__NoACK);
                goto TX_CALIB_ERROR;
            }
            __handlePARev.Reset();

            PrintListInfo("==>Calibrate PA" + dispNum.ToString() + " Success!");
            PrintListInfo("============================================");
            return true;

TX_CALIB_ERROR:

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(2000))
            {
                PrintListInfo(__NoACK);
                goto TX_CALIB_ERROR;
            }
            __handlePARev.Reset();

            PrintListInfo("==>Calibrate PA" + dispNum.ToString() + " Failed!");
            return false;
        }
        /// <summary>发信采样通道
        ///发信采样通道 
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private bool TxSampleChannel(int addr,PowerMeter pm)
        {
            int Lvl = ParameterManage.tx.RFPriority;
            int dispNum = addr - Math.Min(ParameterManage.tx.PA1Addr, ParameterManage.tx.PA2Addr) + 1;

            PrintListInfo("==>Sample...");

            PACalPara paCalPara = null;

            if (addr == ParameterManage.tx.PA1Addr)
            {
                paCalPara = ParameterManage.tx.PA[0];
            }
            else if (addr == ParameterManage.tx.PA2Addr)
            {
                paCalPara = ParameterManage.tx.PA[1];
            }

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFSample2(addr, Lvl);
            RFSignal.RFOpenSource(Lvl);
            RFSignal.RFOn(addr, Lvl);
            RFSignal.RFFreq(addr, Lvl, paCalPara.freq[0]);
            RFSignal.RFPower(addr, Lvl, paCalPara.power[0]);
            RFSignal.RFSample(addr, Lvl);
            RFSignal.RFStart(addr);

            if (__handlePARev.WaitOne(5000) == false)
            {
                PrintListInfo("==>PA NO ACK!");
                goto TX_SAMPLE_ERROR;
            }
            __handlePARev.Reset();

            //采样
            for (int j = 0; j < paCalPara.power.Count; j++)
            {
                for (int i = 0; i < paCalPara.freq.Count; i++)
                {
                    if (!this.__bTxCalRun)
                    {
                        TxCaliStopDuring(addr,pm);                        
                        Thread.CurrentThread.Abort();
                    }

                    PowerStatus status = new PowerStatus();
                    float power = paCalPara.power[j];
                    float freq = paCalPara.freq[i];                    

                    if (addr == ParameterManage.tx.PA1Addr)
                    {
                        power += ParameterManage.tx.powerCalib[0, i, j];
                    }
                    else
                    {
                        power += ParameterManage.tx.powerCalib[1, i, j];
                    }

                    Thread.Sleep(300);
                    RFSignal.RFClear(addr, Lvl);
                    RFSignal.RFFreq(addr, Lvl, freq);
                    RFSignal.RFPower(addr, Lvl, power);
                    RFSignal.RFSetAtt(addr, Lvl, (int)(paCalPara.powerAtt[j] * 2));                 
                    RFSignal.RFStart(addr);

                    if (!__handlePARev.WaitOne(5000))
                    {
                        PrintListInfo("==>PA NO ACK!");
                        goto TX_SAMPLE_ERROR;
                    }
                    __handlePARev.Reset();

                    if (i == 0)
                        Thread.Sleep(2000);

                    int fstnt = ParameterManage.tx.SampleCnt;
                    do
                    {
                        Thread.Sleep(ParameterManage.tx.SampleDelay);

                        RFSignal.RFClear(addr, Lvl);
                        RFSignal.RFSample(addr, Lvl);
                        RFSignal.RFStart(addr);

                        if (!__handlePARev.WaitOne(5000))
                        {
                            PrintListInfo("==>PA NO ACK!");
                            goto TX_SAMPLE_ERROR;
                        }
                        __handlePARev.Reset();
                    } while (i == 0 && --fstnt>0);

                    RFSignal.RFStatus(addr, ref status);
                    PrintListInfo("==>[PA" + dispNum.ToString() + "] Freq = " + 
                                        freq.ToString("F0") + " MHz Power = " + 
                                        power.ToString("F2") + " dBm  OutP = " + 
                                        status.Status2.OutP.ToString("F2") + " dBm" );

                    if (addr == ParameterManage.tx.PA1Addr)
                    {
                        ParameterManage.tx.powerDisp[0, i, j] = paCalPara.power[j] - status.Status2.OutP;
                    }
                    else if (addr == ParameterManage.tx.PA2Addr)
                    {
                        ParameterManage.tx.powerDisp[1, i, j] = paCalPara.power[j] - status.Status2.OutP;
                    }
                }
            }

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(2000))
            {
                PrintListInfo("==>PA NO ACK!");
                goto TX_SAMPLE_ERROR;
            }
            __handlePARev.Reset();

            return true;
        TX_SAMPLE_ERROR:

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!__handlePARev.WaitOne(2000))
            {
                PrintListInfo("==>PA NO ACK!");
            }
            __handlePARev.Reset();

            return false;
        }
        /// <summary>功放功率校准工作线程
        /// 功放功率校准工作线程
        /// </summary>
        /// <param name="o"></param>
        private void TxCalibRun(object o)
        {
            PrintListInfo("=========================================");
            PrintListInfo("==>Start Tx Calibration...");

            //设置功率计
            PowerMeter pm = new PowerMeter(ParameterManage.tx.InsCom, 9600);
            pm.Preset();

            //初始化功放
            RFSignal.InitRFSignal((IntPtr)o);

            //清空错误字典
            //ParameterManage.tx.errCollect1.Clear();
            for (int i = 0; i < ParameterManage.tx.PA.Length; i++)
            {
                for (int j = 0; j < ParameterManage.tx.PA[i].freq.Count; j++)
                {
                    for (int k = 0; k < ParameterManage.tx.PA[i].power.Count; k++)
                    {
                        ParameterManage.tx.errCollect[i, j, k] = false;
                    }
                }
            }

            if (RFSignal.NewRFSignal(ParameterManage.tx.PA1Addr, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintListInfo("==>PA1 Initalization  Failed!");
                goto GOTO_TXCAL_FAILED;
            }

            __handlePARev.WaitOne(2000);
            __handlePARev.Reset();

            if (RFSignal.NewRFSignal(ParameterManage.tx.PA2Addr, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintListInfo("==>PA2 Initalization  Failed!");
                goto GOTO_TXCAL_FAILED; 
            }

            __handlePARev.WaitOne(1000);
            __handlePARev.Reset();

            Thread.Sleep(1000);

            if (ParameterManage.tx.SampleOnly) goto GOTO_ONLY_SAMPLE;

            bool result;

            //耦合参数一校准
            result = TxCalibChannel(ParameterManage.tx.PA1Addr,pm ,TxCalDataStruct.EPowerDivide.low);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxCalibChannel(ParameterManage.tx.PA2Addr, pm,TxCalDataStruct.EPowerDivide.low);
            if (result == false) goto GOTO_TXCAL_FAILED;

            //弹框要求更换耦合器
            if (DialogResult.OK != MessageBox.Show("Please Switch the Coupler!", "", MessageBoxButtons.OKCancel))
            {
                PrintListInfo("==>Tx Calibration Over!");
                goto GOTO_TXCAL_FAILED;
            }

            //耦合参数二校准
            result = TxCalibChannel(ParameterManage.tx.PA1Addr, pm, TxCalDataStruct.EPowerDivide.high);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxCalibChannel(ParameterManage.tx.PA2Addr, pm, TxCalDataStruct.EPowerDivide.high);
            if (result == false) goto GOTO_TXCAL_FAILED;

        GOTO_ONLY_SAMPLE:
            result = TxSampleChannel(ParameterManage.tx.PA1Addr,pm);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxSampleChannel(ParameterManage.tx.PA2Addr,pm);
            if (result == false) goto GOTO_TXCAL_FAILED;

            ReportForm rf = new ReportForm();

            if (DialogResult.OK == rf.ShowDialog())
            {
                ParameterManage.SaveTxChannelPara(Application.StartupPath);
                bool contFlag = true;
                for (int i = 0; i < ParameterManage.tx.PA.Length && contFlag; i++)
                {
                    for (int j = 0; j < ParameterManage.tx.PA[i].freq.Count && contFlag; j++)
                    {
                        for (int k = 0; k < ParameterManage.tx.PA[i].power.Count && contFlag; k++)
                        {
                            if (ParameterManage.tx.errCollect[i, j, k])
                            {
                                ParameterManage.SaveTxFailedData(Application.StartupPath);
                                contFlag = false;
                            }
                        }
                    }
                }
            } 

            RFSignal.RFFinalize();
            pm.Dispose();

            PrintListInfo("==>Calibration is Finished!");
            PrintListInfo("OVER");
            ButtonSwitch(ButtonSwitchStatus.Calibrated);
            this.__bTxCalRun = false;
            return;

GOTO_TXCAL_FAILED:

            RFSignal.RFFinalize();
            pm.Dispose();

            PrintListInfo("==>TX Calibration Failed!");
            PrintListInfo("==>Abort!");
            PrintListInfo("OVER");
            ButtonSwitch(ButtonSwitchStatus.Calibrated);
            this.__bTxCalRun = false;
        }
        #endregion

        #region 发信测试

        /// <summary>
        /// 测试校准
        /// </summary>
        /// <param name="o"></param>
        private void TxCalibTestRun(object o)
        {
            __handlePARev.Reset();

            PowerMeter pm = null;
            int comNum = ParameterManage.tx.PA1Addr;
            int Lvl = ParameterManage.tx.RFPriority;
            float p = float.Parse(tbPower.Text);
            float f = float.Parse(tbfreq.Text);

            PrintListInfo("=========================================");

            if (ParameterManage.tx.SampleOnly)
                PrintListInfo("==>Enter Sample Mode...");
            else
                PrintListInfo("==>Start Test...");

            try
            {
                pm = new PowerMeter(ParameterManage.tx.InsCom, 9600);
                pm.Preset();
                PrintListInfo("==>TEST Power Meter Preset!");
            }
            catch (Exception ex)
            {
                PrintListInfo("==>Open COM Failed! ErrorInfo = "+ex.ToString());
                goto TX_CALIB_TEST_OVER;
            }

            PrintListInfo("==>Open COM Success!");

            RFSignal.InitRFSignal((IntPtr)o);           

            //if (RFSignal.NewRFSignal(comNum, RFSignal.clsSunWave, RFSignal.formuleLinar) == false)
            if (RFSignal.NewRFSignal(comNum, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintListInfo("==>PA1 Initalization  Failed!");
                goto TX_CALIB_TEST_OVER;
            }

            __handlePARev.WaitOne(2000);
            __handlePARev.Reset();

            PrintListInfo("==>PA1 Initalization  Success!");

            float pwrAtt = ParameterManage.tx.PA[0].powerAtt[0];
            for (int i = 1; i < ParameterManage.tx.PA[0].power.Count; i++)
            {
                if (p >= ParameterManage.tx.PA[0].power[i])
                {
                    pwrAtt = ParameterManage.tx.PA[0].powerAtt[i];
                    break;
                }
            }

            PowerStatus status = new PowerStatus();
            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFSample2(comNum, Lvl);
            RFSignal.RFOpenSource(Lvl);
            RFSignal.RFOn(comNum, Lvl);
            RFSignal.RFFreq(comNum, Lvl, f);
            RFSignal.RFPower(comNum, Lvl, p);
            RFSignal.RFSetAtt(comNum, Lvl, (int)(pwrAtt * 2));
            RFSignal.RFStart(comNum);

            if (__handlePARev.WaitOne(5000) == false)
            {
                PrintListInfo("==>PA1 Open Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            __handlePARev.Reset();

            PrintListInfo("==>PA1 Set Output Power = " + p.ToString() + "dBm");

            Thread.Sleep(ParameterManage.tx.SampleDelay);  

            PrintListInfo("==>PA1 Sample...");
            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFSample(comNum, Lvl);
            RFSignal.RFStart(comNum);

            if (__handlePARev.WaitOne(5000) == false)
            {
                PrintListInfo("==>PA1 Read Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            __handlePARev.Reset();

            RFSignal.RFStatus(comNum, ref status);                      
            PrintListInfo("==>Read Sample PA1 Out Power = " + status.Status2.OutP.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Current = " + status.Status1.CurrMax.ToString("F2") + "A");
            //PrintInfo2UI("==>Get PA Addr = " + status.Status1.Adrr.ToString() + "");
            //PrintInfo2UI("==>Get PA Switch = " + status.Status2.RFOn.ToString() + "");
            //PrintInfo2UI("==>Get PA RftP = " + status.Status2.RftP.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Freq = " + status.Status2.Freq.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Temp = " + status.Status2.Temp.ToString("F2") + "dBm");                        

            float power = pm.Read(float.Parse(tbfreq.Text));

            if (power == PowerMeter.READ_ERROR && ParameterManage.tx.SampleOnly == false)
            {
                PrintListInfo("==>Read Instrument Failed!");
                goto TX_CALIB_TEST_OVER;
            }

            power += (p <= ParameterManage.tx.PowerOffsetSwitch)? ParameterManage.tx.PowerOffsetLow:ParameterManage.tx.PowerOffsetHigh;

            PrintListInfo("==>TEST Power Meter Read Power = " + power.ToString() + " dBm"); 
         
            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFOff(comNum, Lvl);
            RFSignal.RFStart(comNum);

            if (__handlePARev.WaitOne(1000) == false)
            {
                PrintListInfo("==>PA1 Close Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            __handlePARev.Reset();

            this.__txStartValid = true;
            this.btnTxStart.Enabled = true;

            PrintListInfo("==>TX Test Over!");
            RFSignal.RFFinalize();
            if (pm != null)
                pm.Dispose();
            ButtonSwitch(ButtonSwitchStatus.CheckPass);
            PrintListInfo("OVER");
            return;

TX_CALIB_TEST_OVER:

            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFOff(comNum, Lvl);
            RFSignal.RFStart(comNum);

            __handlePARev.WaitOne(1000);
            __handlePARev.Reset();

            PrintListInfo("==>TX Test Over!");
            RFSignal.RFFinalize();
            if ( pm!= null )
                pm.Dispose();
            ButtonSwitch(ButtonSwitchStatus.CheckFailed);
            PrintListInfo("OVER");
        }

        #endregion

        #endregion

        #region 频谱仪校准
        /// <summary>
        /// 频谱仪通路Rx校准
        /// </summary>
        /// <param name="o"></param>
        private void RxCalibRun(object o)
        {            
            //Spectrum specObj = new Spectrum(0, (IntPtr)o);
            SignalGenerator sigGen = new SignalGenerator(ParameterManage.rx.insCom,9600);

            int gpioHandle =  Gpio.GPIO_Open();
            int gpioValue;
            bool isSpecStart = false;

            PrintListInfo("=========================================");
            PrintListInfo("==>Start Rx Calibraion...");

            __handleSpecNormal.Reset();
            __handleSpecError.Reset();

            ////将连接操作放入频谱仪检测当中（频谱仪操作前必须进行连接）
            //try
            //{
            //    PrintInfo2UI("==>Spectrum Connecting...");
            //    specObj.Connecting(); 
            //}
            //catch (Exception ex)
            //{
            //    PrintInfo2UI("==>Connect Spectrum Failed! ErrorInfo = "+ex.ToString());
            //    goto GOTO_SPEC_OVER;
            //}
            //PrintInfo2UI("==>Connect Spectrum Success!");

            sigGen.Preset();           
            Thread.Sleep(2000);
            PrintListInfo("==>SignalGenerator has preseted.");
            sigGen.WriteFreq(1e9f);
            sigGen.WritePower(-80f);
            sigGen.Open();         

            for (int i = ParameterManage.rx.startIdx; i < ParameterManage.rx.channel.Length; i++)
            {
                PrintListInfo("===================================================");
                __handleSpecNormal.Reset();
                //PIM
                if (i == ParameterManage.INDEX_PIM)
                {
                    if (DialogResult.OK != MessageBox.Show("Please Connect the PIM Channel!", "Warning", MessageBoxButtons.OKCancel))
                    {
                        goto GOTO_SPEC_OVER;
                    }

                    gpioValue = Gpio.GPIO_Get(gpioHandle, 4);
                    Gpio.GPIO_Set(gpioHandle, 4, 1);
                    PrintListInfo("==>Switch PIM Channel...");
                }
                //窄带
                if (i == ParameterManage.INDEX_NARROW)
                {
                    if (DialogResult.OK != MessageBox.Show("Please Connect the Narrow Channel!","Warning",MessageBoxButtons.OKCancel))
                    {
                        goto GOTO_SPEC_OVER;
                    }

                    gpioValue = Gpio.GPIO_Get(gpioHandle, 4);
                    Gpio.GPIO_Set(gpioHandle, 4, 1);
                    PrintListInfo("==>Switch Narrow Channel...");
                }
                //宽带
                if (i == ParameterManage.INDEX_BROAD)
                {
                    if (DialogResult.OK != MessageBox.Show("Please Connect the Broad Channel!", "Warning", MessageBoxButtons.OKCancel))
                    {
                        goto GOTO_SPEC_OVER;
                    }

                    gpioValue = Gpio.GPIO_Get(gpioHandle, 4);
                    Gpio.GPIO_Set(gpioHandle, 4, 0);
                    PrintListInfo("==>Switch Broad Channel...");
                }

                RxCalDataStruct.bandChannel chanRx = ParameterManage.rx.channel[i];

                //打开频谱仪进行分析
                __specObj.StartAnalysis(    chanRx.start,
                                                            chanRx.stop,
                                                            chanRx.att,
                                                            chanRx.rbw[0],
                                                            chanRx.vbw,
                                                            chanRx.span[0]);

                isSpecStart = true;

                PrintListInfo("==>Configurate the Spectrum...");

                Thread.Sleep(1000);//需延时

                for (int j = 0; j < chanRx.rbw.Count; j++)
                {
                    PrintListInfo("--------------------------------------------------------------------------");
                    int rbw = chanRx.rbw[j]*1000;
                    
                    for (int k = 0; k < chanRx.freq.Count; k++)
                    {
                        if (!this.__bRxCalRun)
                        {
                            __specObj.StopAnalysis();
                            __specObj.Dispose();
                            Gpio.GPIO_Close(gpioHandle);
                            sigGen.Close();                           
                            this.Invoke(new MethodInvoker(delegate { this.Close(); }));
                            Thread.CurrentThread.Abort();
                        }

                        PrintListInfo("+++++++++++++++++++++++++++++++++++++++++++++++++++");
                        float freq = chanRx.freq[k];
                        float power = chanRx.power[k];
                        PrintListInfo("==>Set SignalGenerator Power = " + power.ToString() + " dBm / freq = " + freq.ToString() + " MHz");

                        //宽带
                        __specObj.StopAnalysis();
                        Thread.Sleep(100);
                        __specObj.StartAnalysis(  (int)(chanRx.freq[k]*1000-500),
                                                                    (int)(chanRx.freq[k]*1000+500),
                                                                    chanRx.att,
                                                                    rbw,
                                                                    chanRx.vbw,
                                                                    chanRx.span[0]);
                        
                        //设置信号源
                        sigGen.WritePower(power);
                        Thread.Sleep(10);
                        sigGen.WriteFreq(freq);

                        //Thread.Sleep(100);
                        //if (power != sigGen.ReadPower())
                        //{
                        //    PrintInfo2UI("==>(Power)Read SignalGenerator Failed!");
                        //    goto GOTO_SPEC_OVER;
                        //}
                       
                        //Thread.Sleep(100);
                        //if (freq != sigGen.ReadFreq())
                        //{
                        //    PrintInfo2UI("==>(Frequence)Read SignalGenerator Failed!");
                        //    goto GOTO_SPEC_OVER;
                        //}
                        //PrintInfo2UI("==>(Frequence)" + freq.ToString() + " Read SignalGenerator freq = " + sigGen.ReadFreq().ToString());

                        PrintListInfo("==>Set Sepctrum RBW = " + (rbw/1000).ToString() + " KHz");
                        __specObj.SetRBW(rbw);

                        if (__handleSpecError.WaitOne(1000) == true)
                        {
                            PrintListInfo("==>Spectrum has errors!");
                            goto GOTO_SPEC_OVER;
                        }                        

                        Thread.Sleep(500);
                        
                        float dbm = 0;
                        do
                        {
                            if (__handleSpecNormal.WaitOne(5000) == false)
                            {
                                PrintListInfo("==>Spectrum without response!");
                                goto GOTO_SPEC_OVER;
                            }
                            __handleSpecNormal.Reset();

                            dbm = __specObj.FindMaxValue();
                                 
                        } while (Math.Abs(dbm - power) > 100);

                        if (j == 0 && k == 0)
                        {
                            __specObj.StopAnalysis();
                            Thread.Sleep(100);
                            __specObj.StartAnalysis((int)(chanRx.freq[k] * 1000 - 2000),
                                                                       (int)(chanRx.freq[k] * 1000 + 2000),
                                                                        chanRx.att,
                                                                        rbw,
                                                                        chanRx.vbw,
                                                                        chanRx.span[0]);

                            Thread.Sleep(1000);
                            if (__handleSpecNormal.WaitOne(5000) == false)
                            {
                                PrintListInfo("==>Spectrum without response!");
                                goto GOTO_SPEC_OVER;
                            }
                            __handleSpecNormal.Reset();

                            dbm = __specObj.FindMaxValue();
                        }

                        //读取频谱仪测到的功率值

                        chanRx.powerCal[j, k] = power - dbm;
                        PrintListInfo("==>Read from Sepctrum is Power = " + dbm.ToString() + " dBm");                       
                    }
                }

                __specObj.StopAnalysis();                
            }
            
            Gpio.GPIO_Close(gpioHandle);
            sigGen.Close();
            sigGen.Dispose();
            ButtonSwitch(ButtonSwitchStatus.Calibrated);

            if( DialogResult.OK ==  MessageBox.Show("Save the calibration data","",MessageBoxButtons.OKCancel))
                ParameterManage.SaveRxChannelPara(Application.StartupPath);

            PrintListInfo("==>Calibrate Rx Success!");
            PrintListInfo("OVER");
            return;

        GOTO_SPEC_OVER:
            if (isSpecStart)
                __specObj.StopAnalysis();            
            Gpio.GPIO_Close(gpioHandle);
            sigGen.Close();
            sigGen.Dispose();
            ButtonSwitch(ButtonSwitchStatus.Calibrated);

            PrintListInfo("==>Calibrate Rx Failed!");
            PrintListInfo("OVER");
        }
        /// <summary>
        /// 检测接收通道
        /// </summary>
        /// <param name="o"></param>
        private void RxCalibTestRun(object o)
        {
            int gpioHandle = Gpio.GPIO_Open();
            float p = float.Parse(this.tbPower.Text);
            float f = float.Parse(this.tbfreq.Text);
            bool isSpecStart = false;
            SignalGenerator sgObj = null;

            PrintListInfo("=========================================");
            PrintListInfo("==>TEST:Start...");

            __handleSpecNormal.Reset();

            try
            {
                sgObj = new SignalGenerator(ParameterManage.rx.insCom, 9600);
            }
            catch (Exception ex)
            {
                PrintListInfo("==>TEST:Open COM Interface Failed! Error = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            sgObj.Preset();
            Thread.Sleep(1500);
            PrintListInfo("==>TEST:SignalGenerator Preset!");
            sgObj.WriteFreq(f);
            sgObj.WritePower(p);
            sgObj.Open();
            PrintListInfo("==>TEST:SignalGenerator Open!");
         
            if (this.rbPim.Checked)
            {
                Gpio.GPIO_Set(gpioHandle, 4, 1);
            }
            else if (this.rbNarrow.Checked)
            {
                Gpio.GPIO_Set(gpioHandle, 4, 1);
            }
            else if (this.rbBroad.Checked)
            {
                Gpio.GPIO_Set(gpioHandle, 4, 0);
            }

            try
            {
                PrintListInfo("==>TEST:Spectrum Connecting...");
                if (__specObj == null)
                {//只初始化连接一次，因为频谱仪库函数使用长连接方式
                    __specObj = new Spectrum(0, (IntPtr)o);
                    __specObj.Connecting();
                }
            }
            catch (Exception ex)
            {
                PrintListInfo("==>TEST:Connect Spectrum Failed! ErrorInfo = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            PrintListInfo("==>TEST:Spectrum Connected!");

            try
            {
                //打开频谱仪进行分析
                __specObj.StartAnalysis(
                                                    (int)(f*1000-2000),
                                                    (int)(f*1000+2000),
                                                    0,
                                                    1000,
                                                    1000,
                                                    200);
                isSpecStart = true;
            }
            catch (Exception ex)
            {
                PrintListInfo("==>TEST:Spectrum Start Failed! Error = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            if (__handleSpecNormal.WaitOne(5000) == false)
            {
                PrintListInfo("==>TEST:Fetch Sepctrum Data Failed!");
                goto GOTO_RX_CHECK_FAILED;
            }
            __handleSpecNormal.Reset();

            float rdValue = __specObj.FindMaxValue();

            PrintListInfo("==>TEST:Spectrum Read Value = " + rdValue.ToString() + " dBm");

            if (Math.Abs(rdValue - p) > 30f)
            {
                MessageBox.Show("Please make sure of you have connected the Signal Generator!");
                goto GOTO_RX_CHECK_FAILED;
            }

            PrintListInfo("==>TEST:Close SignalGenerator & Spectrum!");                       
            __specObj.StopAnalysis();
            sgObj.Dispose();
            Gpio.GPIO_Close(gpioHandle);
            ButtonSwitch(ButtonSwitchStatus.CheckPass);
            PrintListInfo("==>TEST:RX Check Success!");
            PrintListInfo("OVER");
            return;

GOTO_RX_CHECK_FAILED:
            sgObj.Dispose();
            if(isSpecStart)
                __specObj.StopAnalysis();
            Gpio.GPIO_Close(gpioHandle);
            ButtonSwitch(ButtonSwitchStatus.CheckFailed);
            PrintListInfo("==>TEST:RX Check Failed!");
            PrintListInfo("OVER");
        }
        #endregion

        #region UI元素

        /// <summary>
        /// 更新UI
        /// </summary>
        /// <param name="str"></param>
        private void PrintListInfo(string str)
        {
            //Action<string> abc;
            //abc = delegate(string s)
            //{
            //    if (s == "OVER")
            //    {
            //        this.toolStripStatusLabel1.Text = string.Empty;
            //        this.timeCnt = 0;
            //        this.timer1.Enabled = false;
            //        return;
            //    }
            //    this.toolStripStatusLabel1.Text = "Running...";
            //    this.listBox1.Items.Add(s);
            //    this.listBox1.SelectedIndex = listBox1.Items.Count - 1;
            //};
            //this.Invoke(new Action<string>(abc), new object[] { str });

            this.Invoke(new MethodInvoker(delegate
            {
                if (str == "OVER")
                {
                    this.toolStripStatusLabel1.Text = string.Empty;
                    this.__timeCnt = 0;
                    this.timer1.Enabled = false;
                    return;
                }

                this.toolStripStatusLabel1.Text = "Running...";
                this.listBox1.Items.Add(str);
                this.listBox1.SelectedIndex = listBox1.Items.Count - 1;

            }));
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {            
            if (this.cbbCom.Text == string.Empty)
            {
                MessageBox.Show("Please Select the COM!");
                return;
            }

            if (this.rbTX.Checked)
            {
                //Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
                if (DialogResult.OK != MessageBox.Show("Make Sure You Have Connect to the Power Meter!", "Message Content", MessageBoxButtons.OKCancel))
                    return;
                Thread trd = new Thread(TxCalibTestRun);
                trd.Start(this.Handle as object);
            }
            else
                if (rbRX.Checked)
                {
                    if (DialogResult.OK != MessageBox.Show("Make Sure You Have Connect to the Signal Generatior", "Message Content", MessageBoxButtons.OKCancel))
                        return;
                    Thread trd = new Thread(RxCalibTestRun);
                    trd.Start(this.Handle as object);
                }

            this.timer1.Enabled = true;

            ButtonSwitch(ButtonSwitchStatus.Checking);            
        }

        private void btnTxStart_Click(object sender, EventArgs e)
        {
            if (rbTX.Checked)
            {
                this.__bTxCalRun = true;
                Thread trd = new Thread(TxCalibRun);
                trd.Start(this.Handle as object);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(TxCalibRun), this.Handle as object);
            }
            else  if (rbRX.Checked)
            {
                this.__bRxCalRun = true;
                Thread trd = new Thread(RxCalibRun);
                trd.Start(this.Handle as object);
            }

            this.timer1.Enabled = true;

            ButtonSwitch(ButtonSwitchStatus.Calibrating);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox1 abb = new AboutBox1();
            abb.StartPosition = FormStartPosition.CenterScreen;
            abb.Show();
        }      

        private void ButtonSwitch(ButtonSwitchStatus b)
        {
            Action<ButtonSwitchStatus> abc;
            abc = delegate(ButtonSwitchStatus bss)
            {
                switch (bss)
                {
                    case ButtonSwitchStatus.Ready:
                        this.btnTxStart.Enabled = false;
                        this.btnTxTest.Enabled = true;
                        break;
                    case ButtonSwitchStatus.Checking:
                        this.btnTxStart.Enabled = false;
                        this.btnTxTest.Enabled = false;
                        break;
                    case ButtonSwitchStatus.CheckPass:
                        this.btnTxStart.Enabled = true;
                        this.btnTxTest.Enabled = true;
                        break;
                    case ButtonSwitchStatus.CheckFailed:
                        this.btnTxStart.Enabled = false;
                        this.btnTxTest.Enabled = true;
                        break;
                    case ButtonSwitchStatus.Calibrating:
                        this.btnTxStart.Enabled = false;
                        this.btnTxTest.Enabled = false;
                        break;
                    case ButtonSwitchStatus.Calibrated:
                        this.btnTxStart.Enabled = false;
                        this.btnTxTest.Enabled = true;
                        break;
                    default:
                        break;
                }
            };
            this.Invoke(abc, new object[] { b });
        }

        private void cbbCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            ParameterManage.tx.InsCom = this.cbbCom.Text;
            ParameterManage.rx.insCom = this.cbbCom.Text;
        }

        private void cbbCom_MouseClick(object sender, MouseEventArgs e)
        {
            ComboBox cbb = sender as ComboBox;
            cbb.Items.Clear();
            cbb.Items.AddRange(SerialPort.GetPortNames() as object[]);
            //ComMask
            foreach (var item in ParameterManage.ci.comMask)
            {
                cbb.Items.Remove(item);
            }
        }

        private void rbRX_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb.Checked)
            {
                this.rbPim.Visible = true;
                this.rbNarrow.Visible = true;
                this.rbBroad.Visible = true;
                this.tbDisplay.Text = ParameterManage.rxInfo;
                this.listBox1.Items.Clear();
                this.tbPower.Text = ParameterManage.rx.channel[0].power[0].ToString();
                this.tbfreq.Text = ParameterManage.rx.channel[0].freq[0].ToString();
                ButtonSwitch(ButtonSwitchStatus.Ready);
            }
        }

        private void rbTX_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton tb = sender as RadioButton;
            if (tb.Checked)
            {
                this.rbPim.Visible = false;
                this.rbNarrow.Visible = false;
                this.rbBroad.Visible = false;
                this.tbDisplay.Text = ParameterManage.txInfo;
                this.listBox1.Items.Clear();
                this.tbPower.Text = ParameterManage.tx.PA[0].power[0].ToString();
                this.tbfreq.Text = ParameterManage.tx.PA[0].freq[0].ToString();
                ButtonSwitch(ButtonSwitchStatus.Ready);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.__timeCnt++;
            this.toolStripStatusLabel2.Text = (this.__timeCnt / 60).ToString("D2") + ":" + (this.__timeCnt % 60).ToString("D2");
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbg = new FolderBrowserDialog();
            if (DialogResult.OK == fbg.ShowDialog())
            {
                ParameterManage.ci.storePath = fbg.SelectedPath;
                ParameterManage.SaveConfiguration(Application.StartupPath);
            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            //this.listBox1.DrawMode = DrawMode.OwnerDrawFixed;  使用自绘需要在初始化时设置该模式
            if (e.Index < 0)return;

            string s = this.listBox1.Items[e.Index].ToString();
            //Font fontTemp = this.Font.Clone() as Font;

            Font fontTemp = this.listBox1.Font as Font;

            if (s.Contains("Failed") || s.Contains(__NoACK))
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Black), e.Bounds);
                e.Graphics.DrawString(s, this.Font, Brushes.Red, e.Bounds);
                e.DrawFocusRectangle();
            }
            else if (s.Contains("Success"))
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Beige), e.Bounds);
                e.Graphics.DrawString(s, this.Font, Brushes.Green, e.Bounds);
                e.DrawFocusRectangle();
            }
            else
            {
                e.Graphics.DrawString(s, fontTemp, new SolidBrush(this.ForeColor), e.Bounds);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(DialogResult.OK !=  MessageBox.Show(this,"确定退出",null,MessageBoxButtons.OKCancel,MessageBoxIcon.Question))return;

            ToolStripMenuItem msmi = sender as ToolStripMenuItem;
            msmi.Enabled = false;

            if (__specObj != null)
            {
                __specObj = null;
            }

            if (!this.__bTxCalRun && !this.__bRxCalRun)
            {
                this.Close();
            }

            lock (this.__threadLock)
            {
                this.__bTxCalRun = false;
                this.__bRxCalRun = false;
            }
        }
        #endregion
    }
}
