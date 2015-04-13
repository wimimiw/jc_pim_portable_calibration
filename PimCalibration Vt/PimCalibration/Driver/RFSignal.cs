using System;
using System.Runtime.InteropServices;

namespace PimCalibration
{
    /// <summary>
    /// 功放命令优先级，值越小优先级越高
    /// </summary>
    internal class RFPriority 
    {
        private RFPriority()
        {
            //
        }
        public const int LvlOne = 1;
        public const int LvlTwo = 2;
    }    

    /// <summary>
    /// 功放设备异常类型
    /// </summary>
    internal class RFErrors
    {
        /// <summary>
        /// 功放电流异常
        /// </summary>
        public bool  RF_CurrError;
        public float RF_CurrValue;

        /// <summary>
        /// 功放温度异常
        /// </summary>
        public bool RF_TempError;
        public float RF_TempValue;

        /// <summary>
        /// 功放驻波异常
        /// </summary>
        public bool RF_VswrError;
        public float RF_VswrValue;

        public bool RF_RftErr;
        public int RF_RfValue;


        /// <summary>
        /// 功放通信异常
        /// </summary>
        public bool RF_TimeOut;

        public override string ToString()
        {
            string s = "";

            if (RF_TimeOut)
                s = "RF timeout!\r\n";

            if (RF_VswrError)
                s = s + " Vswr Warning: " + RF_VswrValue.ToString("0.#")+"\r\n";

            if (RF_CurrError)
                s = s + " Curr Warning:" + RF_CurrValue.ToString("0.#") + "\r\n";

            if (RF_TempError)
                s = s + " Temperature Warning: " + RF_TempValue.ToString("0.#")+"\r\n";

            if (RF_RftErr)
                s = s + "RF IS CLOSED!";
            return s;
        }  
    }

    /// <summary>
    /// 频谱设备异常类型
    /// </summary>
    internal class SpectrumErrors
    {
        /// <summary>
        /// 频谱通信异常
        /// </summary>
        public bool Spectrum_TimeOut;
    }

    /// <summary>
    /// 频谱仪器类型指示的枚举字
    /// </summary>
    internal class SpectrumType {
        public const int SPECAT2 = 0;
        public const int IRDSH   = 1;
        public const int Deli = 2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PowerStatus1 
    {
        /// <summary>
        /// 功放地址，0~255
        /// </summary>
        public byte Adrr;       
        
        /// <summary>
        /// 硬件规定字符长度
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] SerNo;

        /// <summary>
        /// 硬件规定字符长度
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Ver;

        /// <summary>
        ///MHz, 精确1位小数 
        /// </summary>
        public float FreqMax;

        /// <summary>
        ///MHz, 精确1位小数 
        /// </summary>
        public float FreqMin;

        /// <summary>
        /// A，精确1位小数
        /// </summary>
        public float CurrMax;

        /// <summary>
        /// A，精确1位小数
        /// </summary>
        public float CurrMin;

        /// <summary>
        /// ℃，精确1位小数
        /// </summary>
        public float TempMax;

        /// <summary>
        /// ℃，精确1位小数
        /// </summary>
        public float TempMin;   
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PowerStatus2 
    {
        /// <summary>
        ///1：失锁，0：不失锁
        /// </summary>
        public byte Locked;

        /// <summary>
        ///1：开启，0：关闭 
        /// </summary>
        public byte RFOn;

        //
        /// <summary>
        ///1: 反射功率异常 0：反射功率正常 
        /// </summary>
        public byte RftErr;

        /// <summary>
        ///1: 电流异常     0：电流正常
        /// </summary>
        public byte CurrErr;

        /// <summary>
        ///1: 温度异常     0：温度正常 
        /// </summary>
        public byte TempErr;

        /// <summary>
        /// 输出功率 dBm, 精确1位小数
        /// </summary>
        public float OutP;

        /// <summary>
        ///反射功率 dBm, 精确1位小数 
        /// </summary>
        public float RftP;

        /// <summary>
        /// MHz, 精确1位小数
        /// </summary>
        public float Freq;

        /// <summary>
        /// A，精确1位小数
        /// </summary>
        public float Current;

        /// <summary>
        /// ℃，精确1位小数 
        /// </summary>
        public float Temp;

        /// <summary>
        /// 精确1位小数
        /// </summary>
        public float Vswr;        
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PowerStatus
    { 
        /// <summary>
        /// 固定（全局）功放状态
        /// </summary>
        public PowerStatus1 Status1;

        /// <summary>
        /// 常用功放状态信息
        /// </summary>
        public PowerStatus2 Status2;
    }     
   
    internal class RFSignal
    {
        public static readonly int clsSunWave = 0;

        public static readonly int clsJointCom = 1;

        public static readonly int formuleLog = 2;

        public static readonly int formuleLinar = 1;

        private static int TimeOut = 1000;

        private static readonly int ComCount = 8;

        private static string[] ComNames = { "COM1", "COM2", "COM3", "COM4",
                                             "COM5", "COM6", "COM7", "COM8",
                                             "COM9", "COM10", "COM11", "COM12",
                                             "COM13", "COM14", "COM15", "COM16"
                                           };

        public static void InitRFSignal(IntPtr destHandle)
        {
            RFSignal.RFInitialize(ComCount,
                                  destHandle,
                                  MessageID.RF_SUCCED_ALL,
                                  MessageID.RF_SUCCED_ONE,
                                  MessageID.RF_FAILED);
        }

        public static void FinaRFSignal()
        {
            RFSignal.RFFinalize();
        }

        public static bool NewRFSignal(int comAddr, 
                                       int clsValue, 
                                       int forValue)
        {
            bool flag = false;
            int lvl = RFPriority.LvlTwo;
            RFSignal.RFNewSignal(comAddr,
                                 ComNames[comAddr - 1],
                                 clsValue,
                                 forValue);

            if (RFSignal.RFConnected(comAddr, TimeOut))
            {
                flag = true;
                RFSignal.EnableLog(true);

                RFSignal.RFClear(comAddr, lvl);

                //只有三维功放需要执行总查询
                if ( clsValue == clsSunWave )//if (App_Configure.Cnfgs.RFClass == clsSunWave)
                {
                    //第一次必须做总查询获取功率斜率和一个定标值
                    RFSignal.RFSample2(comAddr, lvl);
                    RFSignal.RFPower(comAddr, lvl, 30);
                }

                RFSignal.RFSample(comAddr, lvl);
                RFSignal.RFStart(comAddr);
            }
            return flag;
        }

        #region 引入功放动态库函数
        /// <summary>
        /// 设置功放衰减(Att)
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="ComName"></param>
        /// <param name="RFClass"></param>
        /// <param name="RFFormula"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFSetAtt")]
        public static extern void RFSetAtt(int ComAddr,
                                              int priority,
                                              int value);
        /// <summary>
        /// 设置功放信源开
        /// </summary>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFOpenSource")]
        public static extern void RFOpenSource(int priority);

        /// <summary>
        /// 设置功放信源关
        /// </summary>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFCloseSource")]
        public static extern void RFCloseSource(int priority);

        /// <summary>
        /// 建立指定长度的功放对象列表，并设置目标窗体句柄和消息号
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="hwnd"></param>
        /// <param name="wmRFsuccAll"></param>
        /// <param name="wmRFsuccOne"></param>
        /// <param name="wmRFfailed"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFInitialize")]        
        public static extern void RFInitialize(int capacity,
                                               IntPtr hwnd,
                                               int  wmRFsuccAll,
                                               int  wmRFsuccOne,
                                               int  wmRFfailed);
        
        /// <summary>
        /// 释放功放对象列表，并关闭所有活动的功放线程
        /// </summary>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFFinalize")]
        public static extern void RFFinalize();

        /// <summary>
        /// 激活功放对象列表中的某个功放
        /// 其特征表述：地址为ComAddr，名称为ComName，类型为RFClass，功率计算方式为RFFormula
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="ComName"></param>
        /// <param name="RFClass"></param>
        /// <param name="RFFormula"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFNewSignal")]
        public static extern void RFNewSignal(int ComAddr,
                                              string ComName,
                                              int clsValue,
                                              int forValue);
        /// <summary>
        /// 查询地址为ComAddr的功放被激活成功，TRUE成功，FALSE失败
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <returns></returns>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFConnected")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RFConnected(int ComAddr, 
                                              int Timeout);

        /// <summary>
        /// 查询地址为ComAddr的功放状态信息
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="dest"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStatus")]
        public static extern void RFStatus(int ComAddr, 
                                           ref PowerStatus dest);

        /// <summary>
        /// 清楚地址为ComAddr功放的动作（命令）列表
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFClear")]
        public static extern void RFClear(int ComAddr, 
                                          int priority);

       /// <summary>
       /// 设置地址为ComAddr的功放中心频率，单位MHz
       /// </summary>
       /// <param name="ComAddr"></param>
       /// <param name="priority"></param>
       /// <param name="f"></param>
       /// <param name="?"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFFreq")]
        public static extern void RFFreq(int ComAddr,
                                         int priority,
                                         float f);

        /// <summary>
        /// 设置地址为ComAddr的功放输出功率，单位dBm
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        /// <param name="p"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFPower")]
        public static extern void RFPower(int ComAddr, 
                                          int priority, 
                                          float p);

        /// <summary>
        /// 设置地址为ComAddr的功放输出功率和中心频率，单位dBm、MHz
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        /// <param name="p"></param>
        /// <param name="f"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFPowerFreq")]
        public static extern void RFPowerFreq(int ComAddr, 
                                              int priority,
                                              float p, 
                                              float f);
       
        /// <summary>
        /// 开启地址为ComAddr的功放，功放将以当前设置值启动
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFOn")]
        public static extern void RFOn(int ComAddr, 
                                       int priority);

        /// <summary>
        /// 关闭地址为ComAddr的功放
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFOff")]
        public static extern void RFOff(int ComAddr, 
                                        int priority);

        /// <summary>
        /// 设置地址为ComAddr的功放输出功率和中心频率，单位dBm、MHz；并开启功放
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        /// <param name="p"></param>
        /// <param name="f"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFOnWith")]
        public static extern void RFOnWith(int ComAddr,
                                           int priority,
                                           float p,
                                           float f);

        /// <summary>
        /// 对地址为ComAddr的功放执行部分采样查询，获取功放关键的状态信息
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFSample")]
        public static extern void RFSample(int ComAddr,
                                           int priority);

        /// <summary>
        /// 对地址为ComAddr的功放执行完全采样查询，获取功放全部的状态信息
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFSample2")]
        public static extern void RFSample2(int ComAddr,
                                           int priority);

        /// <summary>
        /// 请求对地址为ComAddr的功放执行发送命令动作
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStart")]
        public static extern void RFStart(int ComAddr);

        /// <summary>
        /// 使串口主线程在执行完当前的命令后，就抛弃命令队列中的后续命令
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStop")]
        public static extern void RFStop(int ComAddr);

        /// <summary>
        /// 启用或关闭日志记录功能，TRUE 启用 FALSE 关闭 
        /// </summary>
        /// <param name="value"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetWndHandle")]
        public static extern void SetWndHandle(IntPtr hwnd);

        /// <summary>
        /// 启用或关闭日志记录功能，TRUE 启用 FALSE 关闭 
        /// </summary>
        /// <param name="value"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "EnableLog")]
        public static extern void EnableLog([MarshalAs(UnmanagedType.Bool)] bool value);
        #endregion
    }

}
