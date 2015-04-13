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

namespace PimCalibration
{
    public partial class MainForm : Form
    {
        #region 变量
        private ManualResetEvent handleSpecNormal = new ManualResetEvent(false);
        private ManualResetEvent handleSpecError = new ManualResetEvent(false);
        private ManualResetEvent handlePARev = new ManualResetEvent(false);
        //private ManualResetEvent handleThrdAbort = new ManualResetEvent(false);
        private bool txStartValid = false;
        //private bool rxStartValid = false;
        private int timeCnt = 0;
        private Spectrum __specObj = null;        
        private const string NoACK = "==>PA NO ACK!";

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
                MessageBox.Show("tx_calib.ini has error!");
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

            this.Text = ParameterManage.ci.formTitle;
            this.tbDisplay.Text = ParameterManage.txInfo;
            
            this.rbTX.Select();
            this.toolStripStatusLabel1.Text = string.Empty;
            this.toolStripStatusLabel2.Text = string.Empty;

            int freq = ParameterManage.rx.channel[2].stop;

            ButtonSwitch(ButtonSwitchStatus.Ready);
            ParameterManage.tx.RFPriority = RFPriority.LvlTwo;
            //ButtonSwitch(ButtonSwitchStatus.CheckPass);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (__specObj != null)
            {
                __specObj = null;
            }
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
                handleSpecNormal.Set();                
            }
            if (m.Msg == MessageID.SPECTRUM_ERROR)
            {
                handleSpecError.Set();                
            }
            if (m.Msg == MessageID.RF_SUCCED_ALL)
            {
                if (m.WParam.ToInt32() == ParameterManage.tx.PA1Addr)
                {
                    handlePARev.Set();
                }
                if (m.WParam.ToInt32() == ParameterManage.tx.PA2Addr)
                {
                    handlePARev.Set();
                }
            }
            else if (m.Msg == MessageID.RF_ERROR)
            {
                if (m.WParam.ToInt32() == ParameterManage.tx.PA1Addr)
                {
                    handlePARev.Set();
                }

                if (m.WParam.ToInt32() == ParameterManage.tx.PA2Addr)
                {
                    handlePARev.Set();
                }
            }
            else
                base.WndProc(ref m);
        }
        #endregion        

        #region 功放校准
        /// <summary>
        /// 功放通道处理函数
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="pm"></param>
        private bool TxCalibChannel(int addr,PowerMeter pm,TxCalDataStruct.EPowerDivide ed)
        {
            int dispNum = addr - Math.Min(ParameterManage.tx.PA1Addr, ParameterManage.tx.PA2Addr) + 1;
            PrintInfo2UI("==>Start Calibrate PA" + dispNum.ToString()+"...");

            int Lvl = ParameterManage.tx.RFPriority;            
  
            //PowerStatus status = new PowerStatus();
            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFSample2(addr, Lvl);
            RFSignal.RFPower(addr, Lvl, ParameterManage.tx.power[0]);
            RFSignal.RFFreq(addr, Lvl, ParameterManage.tx.freq[0]);
            RFSignal.RFOpenSource(Lvl);
            RFSignal.RFOn(addr, Lvl);            
            RFSignal.RFStart(addr);

            if (!handlePARev.WaitOne(4000))
            {
                PrintInfo2UI(NoACK);
                goto TX_CALIB_ERROR;
            }
            handlePARev.Reset();

            Thread.Sleep(300);

            //找到功率分界点序号
            int pwrIdxStart = 0;
            int pwrIdxEnd = 0;
            int pwrIdxDiv = 0;

            for (int i = 0; i < ParameterManage.tx.power.Count; i++)
            {
                if (ParameterManage.tx.power[i] > ParameterManage.tx.PowerOffsetSwitch)
                {
                    pwrIdxDiv = i;
                    break;
                }
            }

            pwrIdxStart = (ed == TxCalDataStruct.EPowerDivide.low) ? 0 : pwrIdxDiv;
            pwrIdxEnd = (ed == TxCalDataStruct.EPowerDivide.low) ? pwrIdxDiv : ParameterManage.tx.power.Count;
            
            for (int i = 0; i < ParameterManage.tx.freq.Count; i++)
            {
                for (int j = pwrIdxStart; j < pwrIdxEnd; j++)
                {
                    int cycleCnt = 0;
                    int calibLarge = 2;
                    float power = ParameterManage.tx.power[j];
                    float powerRead = power;
                    float freq = ParameterManage.tx.freq[i];
                    bool calibOK = true;

                    RFSignal.RFClear(addr, Lvl);
                    RFSignal.RFSetAtt(addr, Lvl, (int)(ParameterManage.tx.powerAtt[j]*2));
                    RFSignal.RFStart(addr);

                    if (!handlePARev.WaitOne(2000))
                    {
                        PrintInfo2UI(NoACK);
                        goto TX_CALIB_ERROR;
                    }
                    handlePARev.Reset();

                    do
                    {
                        if (calibLarge-- > 0)
                            power = power * 2 - powerRead;
                        else
                            power += ParameterManage.tx.Step * (ParameterManage.tx.power[j] - powerRead) / Math.Abs(ParameterManage.tx.power[j] - powerRead);

                        RFSignal.RFClear(addr, Lvl);
                        RFSignal.RFFreq(addr, Lvl, freq);    
                        RFSignal.RFPower(addr, Lvl, power);                                                                               
                        RFSignal.RFStart(addr);

                        if (!handlePARev.WaitOne(2000))
                        {
                            PrintInfo2UI(NoACK);
                            goto TX_CALIB_ERROR;
                        }
                        handlePARev.Reset();

                        Thread.Sleep(ParameterManage.tx.CalDelay);

                        PrintInfo2UI("==>(PA"+dispNum.ToString()+")Frequency:" +
                                             ParameterManage.tx.freq[i] +
                                             "MHz SetPower:" +
                                             power.ToString() + "dBm Try Calibration Count:" + cycleCnt.ToString());
                        
                        //读取功率计
                        powerRead = pm.Read(freq);
                        if (powerRead == PowerMeter.READ_ERROR)
                        {
                            PrintInfo2UI("==>Read Power Failed!");
                            goto TX_CALIB_ERROR;
                        }                        

                        powerRead += (ed == TxCalDataStruct.EPowerDivide.low) ? ParameterManage.tx.PowerOffsetLow : ParameterManage.tx.PowerOffsetHigh;

                        PrintInfo2UI("==>(PA" + dispNum.ToString() + ")Frequency:" +
                                             ParameterManage.tx.freq[i] +
                                             "MHz ReadPower:" +
                                             powerRead.ToString() + "dBm");

                        if (cycleCnt++ > ParameterManage.tx.CycleCnt)
                        {
                            //PrintInfo2UI("==>Calibrate PA" + dispNum.ToString() + " Failure!");
                            //goto TX_CALIB_ERROR;
                            if (addr == ParameterManage.tx.PA1Addr)
                                ParameterManage.tx.errCollect1.Add(ParameterManage.tx.freq[i], ParameterManage.tx.power[j]);
                            if (addr == ParameterManage.tx.PA2Addr)
                                ParameterManage.tx.errCollect2.Add(ParameterManage.tx.freq[i], ParameterManage.tx.power[j]);

                            power = ParameterManage.tx.power[j];
                            calibOK = false;
                            PrintInfo2UI("==>Calibrate PA" + dispNum.ToString() + " Failed!");
                            break;
                        }

                    } while (Math.Abs(ParameterManage.tx.power[j]-powerRead) > ParameterManage.tx.powerFloat[j]);                    
 
                    if (addr == ParameterManage.tx.PA1Addr)
                    {
                        ParameterManage.tx.powerCalib[0, i, j] = power - ParameterManage.tx.power[j];                        
                    }
                    else if (addr == ParameterManage.tx.PA2Addr)
                    {
                        ParameterManage.tx.powerCalib[1, i, j] = power - ParameterManage.tx.power[j];
                    }
                    
                    if(calibOK)
                        PrintInfo2UI("==>(PA" + dispNum.ToString() + ")Frequency:" +
                                        ParameterManage.tx.freq[i] +
                                        "MHz Calibration Success!");
                    else
                        PrintInfo2UI("==>(PA" + dispNum.ToString() + ")Frequency:" +
                                        ParameterManage.tx.freq[i] +
                                        "MHz Calibration Failed!");

                    PrintInfo2UI("---------------------------------------------------------------");
                }
            }

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!handlePARev.WaitOne(2000))
            {
                PrintInfo2UI(NoACK);
                goto TX_CALIB_ERROR;
            }
            handlePARev.Reset();

            PrintInfo2UI("==>Calibrate PA" + dispNum.ToString() + " Success!");
            PrintInfo2UI("============================================");
            return true;

TX_CALIB_ERROR:

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!handlePARev.WaitOne(2000))
            {
                PrintInfo2UI(NoACK);
                goto TX_CALIB_ERROR;
            }
            handlePARev.Reset();

            PrintInfo2UI("==>Calibrate PA" + dispNum.ToString() + " Failed!");
            return false;
        }        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private bool TxSampleChannel(int addr)
        {
            int Lvl = ParameterManage.tx.RFPriority;
            int dispNum = addr - Math.Min(ParameterManage.tx.PA1Addr, ParameterManage.tx.PA2Addr) + 1;

            PrintInfo2UI("==>Sample...");

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFSample2(addr, Lvl);
            RFSignal.RFOpenSource(Lvl);
            RFSignal.RFOn(addr, Lvl);
            RFSignal.RFFreq(addr, Lvl, ParameterManage.tx.freq[0]);
            RFSignal.RFPower(addr, Lvl, ParameterManage.tx.power[0]);
            RFSignal.RFSample(addr, Lvl);
            RFSignal.RFStart(addr);

            if (handlePARev.WaitOne(5000) == false)
            {
                PrintInfo2UI("==>PA NO ACK!");
                goto TX_SAMPLE_ERROR;
            }
            handlePARev.Reset();

            //采样
            for (int j = 0; j < ParameterManage.tx.power.Count; j++)
            {                
                for (int i = 0; i < ParameterManage.tx.freq.Count; i++)
                {
                    PowerStatus status = new PowerStatus();
                    float power = ParameterManage.tx.power[j];
                    float freq = ParameterManage.tx.freq[i];                    

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
                    RFSignal.RFSetAtt(addr, Lvl, (int)(ParameterManage.tx.powerAtt[j] * 2));                 
                    RFSignal.RFStart(addr);

                    if (!handlePARev.WaitOne(5000))
                    {
                        PrintInfo2UI("==>PA NO ACK!");
                        goto TX_SAMPLE_ERROR;
                    }
                    handlePARev.Reset();

                    if (i == 0)
                        Thread.Sleep(2000);

                    int fstnt = ParameterManage.tx.SampleCnt;
                    do
                    {
                        Thread.Sleep(ParameterManage.tx.SampleDelay);

                        RFSignal.RFClear(addr, Lvl);
                        RFSignal.RFSample(addr, Lvl);
                        RFSignal.RFStart(addr);

                        if (!handlePARev.WaitOne(5000))
                        {
                            PrintInfo2UI("==>PA NO ACK!");
                            goto TX_SAMPLE_ERROR;
                        }
                        handlePARev.Reset();
                    } while (i == 0 && --fstnt>0);

                    RFSignal.RFStatus(addr, ref status);
                    PrintInfo2UI("==>PA" + dispNum.ToString() + " Freq = " + freq.ToString() + "MHz,Power = " + power.ToString("F2") + "dBm.OutP= " + status.Status2.OutP.ToString("F2") + "dBm");

                    if (addr == ParameterManage.tx.PA1Addr)
                    {
                        ParameterManage.tx.powerDisp[0, i, j] = ParameterManage.tx.power[j] - status.Status2.OutP;
                    }
                    else if (addr == ParameterManage.tx.PA2Addr)
                    {
                        ParameterManage.tx.powerDisp[1, i, j] = ParameterManage.tx.power[j] - status.Status2.OutP;
                    }
                }
            }

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!handlePARev.WaitOne(2000))
            {
                PrintInfo2UI("==>PA NO ACK!");
                goto TX_SAMPLE_ERROR;
            }
            handlePARev.Reset();

            return true;
        TX_SAMPLE_ERROR:

            RFSignal.RFClear(addr, Lvl);
            RFSignal.RFOff(addr, Lvl);
            RFSignal.RFStart(addr);

            if (!handlePARev.WaitOne(2000))
            {
                PrintInfo2UI("==>PA NO ACK!");
            }
            handlePARev.Reset();

            return false;
        }
        /// <summary>
        /// 功放功率校准工作线程
        /// </summary>
        /// <param name="o"></param>
        private void TxCalibRun(object o)
        {
            PrintInfo2UI("=========================================");
            PrintInfo2UI("==>Start Tx Calibration...");

            //设置功率计
            PowerMeter pm = new PowerMeter(ParameterManage.tx.InsCom, 9600);
            pm.Preset();

            //初始化功放
            RFSignal.InitRFSignal((IntPtr)o);

            //清空错误字典
            ParameterManage.tx.errCollect1.Clear();

            if (RFSignal.NewRFSignal(ParameterManage.tx.PA1Addr, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintInfo2UI("==>PA1 Initalization  Failed!");
                goto GOTO_TXCAL_FAILED;
            }

            handlePARev.WaitOne(2000);
            handlePARev.Reset();

            if (RFSignal.NewRFSignal(ParameterManage.tx.PA2Addr, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintInfo2UI("==>PA2 Initalization  Failed!");
                goto GOTO_TXCAL_FAILED; 
            }

            handlePARev.WaitOne(1000);
            handlePARev.Reset();

            Thread.Sleep(1000);
            if (ParameterManage.tx.SampleOnly) goto GOTO_ONLY_SAMPLE;

            bool result;

            result = TxCalibChannel(ParameterManage.tx.PA1Addr,pm ,TxCalDataStruct.EPowerDivide.low);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxCalibChannel(ParameterManage.tx.PA2Addr, pm,TxCalDataStruct.EPowerDivide.low);
            if (result == false) goto GOTO_TXCAL_FAILED;

            //弹框要求更换耦合器
            if (DialogResult.OK != MessageBox.Show("Please Switch the Coupler!", "", MessageBoxButtons.OKCancel))
            {
                PrintInfo2UI("==>Tx Calibration Over!");
                goto GOTO_TXCAL_FAILED;
            }             

            result = TxCalibChannel(ParameterManage.tx.PA1Addr, pm, TxCalDataStruct.EPowerDivide.high);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxCalibChannel(ParameterManage.tx.PA2Addr, pm, TxCalDataStruct.EPowerDivide.high);
            if (result == false) goto GOTO_TXCAL_FAILED;

        GOTO_ONLY_SAMPLE:
            result = TxSampleChannel(ParameterManage.tx.PA1Addr);
            if (result == false) goto GOTO_TXCAL_FAILED;

            result = TxSampleChannel(ParameterManage.tx.PA2Addr);
            if (result == false) goto GOTO_TXCAL_FAILED;

            //if (DialogResult.OK == MessageBox.Show("Save The Calibration Data!", "", MessageBoxButtons.OKCancel))
            //{
            //    ParameterManage.SaveTxChannelPara(Application.StartupPath);
            //}

            ReportForm rf = new ReportForm();

            if (DialogResult.OK == rf.ShowDialog())
            {
                ParameterManage.SaveTxChannelPara(Application.StartupPath);

                if (ParameterManage.tx.errCollect1.Count > 0 || ParameterManage.tx.errCollect2.Count >0)
                {
                    ParameterManage.SaveTxFailedData(Application.StartupPath);
                }
            }

            RFSignal.RFFinalize();
            pm.Dispose();

            PrintInfo2UI("==>Calibration is Finished!");
            PrintInfo2UI("OVER");
            ButtonSwitch(ButtonSwitchStatus.Calibrated);
            return;

GOTO_TXCAL_FAILED:

            RFSignal.RFFinalize();
            pm.Dispose();

            PrintInfo2UI("==>TX Calibration Failed!");
            PrintInfo2UI("==>Abort!");
            PrintInfo2UI("OVER");
            ButtonSwitch(ButtonSwitchStatus.Calibrated);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        private void TxCalibTestRun(object o)
        {
            handlePARev.Reset();

            PowerMeter pm = null;
            int comNum = ParameterManage.tx.PA1Addr;
            int Lvl = ParameterManage.tx.RFPriority;
            float p = float.Parse(tbPower.Text);
            float f = float.Parse(tbfreq.Text);

            PrintInfo2UI("=========================================");

            if (ParameterManage.tx.SampleOnly)
                PrintInfo2UI("==>Enter Sample Mode...");
            else
                PrintInfo2UI("==>Start Test...");

            try
            {
                pm = new PowerMeter(ParameterManage.tx.InsCom, 9600);
                pm.Preset();
                PrintInfo2UI("==>TEST Power Meter Preset!");
            }
            catch (Exception ex)
            {
                PrintInfo2UI("==>Open COM Failed! ErrorInfo = "+ex.ToString());
                goto TX_CALIB_TEST_OVER;
            }

            PrintInfo2UI("==>Open COM Success!");

            RFSignal.InitRFSignal((IntPtr)o);           

            //if (RFSignal.NewRFSignal(comNum, RFSignal.clsSunWave, RFSignal.formuleLinar) == false)
            if (RFSignal.NewRFSignal(comNum, RFSignal.clsSunWave, ParameterManage.tx.PAformule) == false)
            {
                PrintInfo2UI("==>PA1 Initalization  Failed!");
                goto TX_CALIB_TEST_OVER;
            }

            handlePARev.WaitOne(2000);
            handlePARev.Reset();

            PrintInfo2UI("==>PA1 Initalization  Success!");

            float pwrAtt = ParameterManage.tx.powerAtt[0];
            for (int i = 1; i < ParameterManage.tx.power.Count; i++)
            {
                if (p >= ParameterManage.tx.power[i])
                {
                    pwrAtt = ParameterManage.tx.powerAtt[i];
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

            if (handlePARev.WaitOne(5000) == false)
            {
                PrintInfo2UI("==>PA1 Open Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            handlePARev.Reset();

            PrintInfo2UI("==>PA1 Set Output Power = " + p.ToString() + "dBm");

            Thread.Sleep(ParameterManage.tx.SampleDelay);  

            PrintInfo2UI("==>PA1 Sample...");
            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFSample(comNum, Lvl);
            RFSignal.RFStart(comNum);

            if (handlePARev.WaitOne(5000) == false)
            {
                PrintInfo2UI("==>PA1 Read Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            handlePARev.Reset();

            RFSignal.RFStatus(comNum, ref status);                      
            PrintInfo2UI("==>Read Sample PA1 Out Power = " + status.Status2.OutP.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Current = " + status.Status1.CurrMax.ToString("F2") + "A");
            //PrintInfo2UI("==>Get PA Addr = " + status.Status1.Adrr.ToString() + "");
            //PrintInfo2UI("==>Get PA Switch = " + status.Status2.RFOn.ToString() + "");
            //PrintInfo2UI("==>Get PA RftP = " + status.Status2.RftP.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Freq = " + status.Status2.Freq.ToString("F2") + "dBm");
            //PrintInfo2UI("==>Get PA Temp = " + status.Status2.Temp.ToString("F2") + "dBm");                        

            float power = pm.Read(float.Parse(tbfreq.Text));

            if (power == PowerMeter.READ_ERROR && ParameterManage.tx.SampleOnly == false)
            {
                PrintInfo2UI("==>Read Instrument Failed!");
                goto TX_CALIB_TEST_OVER;
            }

            power += (p <= ParameterManage.tx.PowerOffsetSwitch)? ParameterManage.tx.PowerOffsetLow:ParameterManage.tx.PowerOffsetHigh;

            PrintInfo2UI("==>TEST Power Meter Read Power = " + power.ToString() + " dBm"); 
         
            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFOff(comNum, Lvl);
            RFSignal.RFStart(comNum);

            if (handlePARev.WaitOne(1000) == false)
            {
                PrintInfo2UI("==>PA1 Close Failed!");
                goto TX_CALIB_TEST_OVER;
            }
            handlePARev.Reset();

            this.txStartValid = true;
            this.btnTxStart.Enabled = true;

            PrintInfo2UI("==>TX Test Over!");
            RFSignal.RFFinalize();
            if (pm != null)
                pm.Dispose();
            ButtonSwitch(ButtonSwitchStatus.CheckPass);
            PrintInfo2UI("OVER");
            return;

TX_CALIB_TEST_OVER:

            RFSignal.RFClear(comNum, Lvl);
            RFSignal.RFOff(comNum, Lvl);
            RFSignal.RFStart(comNum);

            handlePARev.WaitOne(1000);
            handlePARev.Reset();

            PrintInfo2UI("==>TX Test Over!");
            RFSignal.RFFinalize();
            if ( pm!= null )
                pm.Dispose();
            ButtonSwitch(ButtonSwitchStatus.CheckFailed);
            PrintInfo2UI("OVER");
        }
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

            PrintInfo2UI("=========================================");
            PrintInfo2UI("==>Start Rx Calibraion...");

            handleSpecNormal.Reset();
            handleSpecError.Reset();

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
            PrintInfo2UI("==>SignalGenerator has preseted.");
            sigGen.WriteFreq(1e9f);
            sigGen.WritePower(-80f);
            sigGen.Open();         

            for (int i = ParameterManage.rx.startIdx; i < ParameterManage.rx.channel.Length; i++)
            {
                PrintInfo2UI("===================================================");
                handleSpecNormal.Reset();
                //PIM
                if (i == ParameterManage.INDEX_PIM)
                {
                    if (DialogResult.OK != MessageBox.Show("Please Connect the PIM Channel!", "Warning", MessageBoxButtons.OKCancel))
                    {
                        goto GOTO_SPEC_OVER;
                    }

                    gpioValue = Gpio.GPIO_Get(gpioHandle, 4);
                    Gpio.GPIO_Set(gpioHandle, 4, 1);
                    PrintInfo2UI("==>Switch PIM Channel...");
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
                    PrintInfo2UI("==>Switch Narrow Channel...");
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
                    PrintInfo2UI("==>Switch Broad Channel...");
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

                PrintInfo2UI("==>Configurate the Spectrum...");

                Thread.Sleep(1000);//需延时

                for (int j = 0; j < chanRx.rbw.Count; j++)
                {
                    PrintInfo2UI("--------------------------------------------------------------------------");
                    int rbw = chanRx.rbw[j]*1000;
                    
                    for (int k = 0; k < chanRx.freq.Count; k++)
                    {
                        PrintInfo2UI("+++++++++++++++++++++++++++++++++++++++++++++++++++");
                        float freq = chanRx.freq[k];
                        float power = chanRx.power[k];
                        PrintInfo2UI("==>Set SignalGenerator Power = " + power.ToString() + " dBm / freq = " + freq.ToString() + " MHz");

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

                        PrintInfo2UI("==>Set Sepctrum RBW = " + (rbw/1000).ToString() + " KHz");
                        __specObj.SetRBW(rbw);

                        if (handleSpecError.WaitOne(1000) == true)
                        {
                            PrintInfo2UI("==>Spectrum has errors!");
                            goto GOTO_SPEC_OVER;
                        }                        

                        Thread.Sleep(500);
                        
                        float dbm = 0;
                        do
                        {
                            if (handleSpecNormal.WaitOne(5000) == false)
                            {
                                PrintInfo2UI("==>Spectrum without response!");
                                goto GOTO_SPEC_OVER;
                            }
                            handleSpecNormal.Reset();

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
                            if (handleSpecNormal.WaitOne(5000) == false)
                            {
                                PrintInfo2UI("==>Spectrum without response!");
                                goto GOTO_SPEC_OVER;
                            }
                            handleSpecNormal.Reset();

                            dbm = __specObj.FindMaxValue();
                        }

                        //读取频谱仪测到的功率值

                        chanRx.powerCal[j, k] = power - dbm;
                        PrintInfo2UI("==>Read from Sepctrum is Power = " + dbm.ToString() + " dBm");                       
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

            PrintInfo2UI("==>Calibrate Rx Success!");
            PrintInfo2UI("OVER");
            return;

        GOTO_SPEC_OVER:
            if (isSpecStart)
                __specObj.StopAnalysis();            
            Gpio.GPIO_Close(gpioHandle);
            sigGen.Close();
            sigGen.Dispose();
            ButtonSwitch(ButtonSwitchStatus.Calibrated);

            PrintInfo2UI("==>Calibrate Rx Failed!");
            PrintInfo2UI("OVER");
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

            PrintInfo2UI("=========================================");
            PrintInfo2UI("==>TEST:Start...");

            handleSpecNormal.Reset();

            try
            {
                sgObj = new SignalGenerator(ParameterManage.rx.insCom, 9600);
            }
            catch (Exception ex)
            {
                PrintInfo2UI("==>TEST:Open COM Interface Failed! Error = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            sgObj.Preset();
            Thread.Sleep(1500);
            PrintInfo2UI("==>TEST:SignalGenerator Preset!");
            sgObj.WriteFreq(f);
            sgObj.WritePower(p);
            sgObj.Open();
            PrintInfo2UI("==>TEST:SignalGenerator Open!");
         
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
                PrintInfo2UI("==>TEST:Spectrum Connecting...");
                if (__specObj == null)
                {//只初始化连接一次，因为频谱仪库函数使用长连接方式
                    __specObj = new Spectrum(0, (IntPtr)o);
                    __specObj.Connecting();
                }
            }
            catch (Exception ex)
            {
                PrintInfo2UI("==>TEST:Connect Spectrum Failed! ErrorInfo = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            PrintInfo2UI("==>TEST:Spectrum Connected!");

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
                PrintInfo2UI("==>TEST:Spectrum Start Failed! Error = " + ex.ToString());
                goto GOTO_RX_CHECK_FAILED;
            }

            if (handleSpecNormal.WaitOne(5000) == false)
            {
                PrintInfo2UI("==>TEST:Fetch Sepctrum Data Failed!");
                goto GOTO_RX_CHECK_FAILED;
            }
            handleSpecNormal.Reset();

            float rdValue = __specObj.FindMaxValue();

            PrintInfo2UI("==>TEST:Spectrum Read Value = " + rdValue.ToString() + " dBm");

            if (Math.Abs(rdValue - p) > 30f)
            {
                MessageBox.Show("Please make sure of you have connected the Signal Generator!");
                goto GOTO_RX_CHECK_FAILED;
            }

            PrintInfo2UI("==>TEST:Close SignalGenerator & Spectrum!");                       
            __specObj.StopAnalysis();
            sgObj.Dispose();
            Gpio.GPIO_Close(gpioHandle);
            ButtonSwitch(ButtonSwitchStatus.CheckPass);
            PrintInfo2UI("==>TEST:RX Check Success!");
            PrintInfo2UI("OVER");
            return;

GOTO_RX_CHECK_FAILED:
            sgObj.Dispose();
            if(isSpecStart)
                __specObj.StopAnalysis();
            Gpio.GPIO_Close(gpioHandle);
            ButtonSwitch(ButtonSwitchStatus.CheckFailed);
            PrintInfo2UI("==>TEST:RX Check Failed!");
            PrintInfo2UI("OVER");
        }
        #endregion

        #region UI元素

        /// <summary>
        /// 更新UI
        /// </summary>
        /// <param name="str"></param>
        private void PrintInfo2UI(string str)
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
                    this.timeCnt = 0;
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
                Thread trd = new Thread(TxCalibRun);
                trd.Start(this.Handle as object);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(TxCalibRun), this.Handle as object);
            }
            else
                if (rbRX.Checked)
                {
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
                this.tbPower.Text = ParameterManage.tx.power[0].ToString();
                this.tbfreq.Text = ParameterManage.tx.freq[0].ToString();
                ButtonSwitch(ButtonSwitchStatus.Ready);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.timeCnt++;
            this.toolStripStatusLabel2.Text = (this.timeCnt / 60).ToString("D2") + ":" + (this.timeCnt % 60).ToString("D2");
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
            string s = this.listBox1.Items[e.Index].ToString();
            //Font fontTemp = this.Font.Clone() as Font;

            Font fontTemp = this.listBox1.Font as Font;

            if (s.Contains("Failed") || s.Contains(NoACK))
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

        #endregion

    }
}
