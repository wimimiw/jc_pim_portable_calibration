using System;
using System.Runtime.InteropServices;

namespace PimCalibration
{
    /// <summary>
    /// �����������ȼ���ֵԽС���ȼ�Խ��
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
    /// �����豸�쳣����
    /// </summary>
    internal class RFErrors
    {
        /// <summary>
        /// ���ŵ����쳣
        /// </summary>
        public bool  RF_CurrError;
        public float RF_CurrValue;

        /// <summary>
        /// �����¶��쳣
        /// </summary>
        public bool RF_TempError;
        public float RF_TempValue;

        /// <summary>
        /// ����פ���쳣
        /// </summary>
        public bool RF_VswrError;
        public float RF_VswrValue;

        public bool RF_RftErr;
        public int RF_RfValue;


        /// <summary>
        /// ����ͨ���쳣
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
    /// Ƶ���豸�쳣����
    /// </summary>
    internal class SpectrumErrors
    {
        /// <summary>
        /// Ƶ��ͨ���쳣
        /// </summary>
        public bool Spectrum_TimeOut;
    }

    /// <summary>
    /// Ƶ����������ָʾ��ö����
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
        /// ���ŵ�ַ��0~255
        /// </summary>
        public byte Adrr;       
        
        /// <summary>
        /// Ӳ���涨�ַ�����
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] SerNo;

        /// <summary>
        /// Ӳ���涨�ַ�����
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Ver;

        /// <summary>
        ///MHz, ��ȷ1λС�� 
        /// </summary>
        public float FreqMax;

        /// <summary>
        ///MHz, ��ȷ1λС�� 
        /// </summary>
        public float FreqMin;

        /// <summary>
        /// A����ȷ1λС��
        /// </summary>
        public float CurrMax;

        /// <summary>
        /// A����ȷ1λС��
        /// </summary>
        public float CurrMin;

        /// <summary>
        /// �棬��ȷ1λС��
        /// </summary>
        public float TempMax;

        /// <summary>
        /// �棬��ȷ1λС��
        /// </summary>
        public float TempMin;   
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PowerStatus2 
    {
        /// <summary>
        ///1��ʧ����0����ʧ��
        /// </summary>
        public byte Locked;

        /// <summary>
        ///1��������0���ر� 
        /// </summary>
        public byte RFOn;

        //
        /// <summary>
        ///1: ���书���쳣 0�����书������ 
        /// </summary>
        public byte RftErr;

        /// <summary>
        ///1: �����쳣     0����������
        /// </summary>
        public byte CurrErr;

        /// <summary>
        ///1: �¶��쳣     0���¶����� 
        /// </summary>
        public byte TempErr;

        /// <summary>
        /// ������� dBm, ��ȷ1λС��
        /// </summary>
        public float OutP;

        /// <summary>
        ///���书�� dBm, ��ȷ1λС�� 
        /// </summary>
        public float RftP;

        /// <summary>
        /// MHz, ��ȷ1λС��
        /// </summary>
        public float Freq;

        /// <summary>
        /// A����ȷ1λС��
        /// </summary>
        public float Current;

        /// <summary>
        /// �棬��ȷ1λС�� 
        /// </summary>
        public float Temp;

        /// <summary>
        /// ��ȷ1λС��
        /// </summary>
        public float Vswr;        
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PowerStatus
    { 
        /// <summary>
        /// �̶���ȫ�֣�����״̬
        /// </summary>
        public PowerStatus1 Status1;

        /// <summary>
        /// ���ù���״̬��Ϣ
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

                //ֻ����ά������Ҫִ���ܲ�ѯ
                if ( clsValue == clsSunWave )//if (App_Configure.Cnfgs.RFClass == clsSunWave)
                {
                    //��һ�α������ܲ�ѯ��ȡ����б�ʺ�һ������ֵ
                    RFSignal.RFSample2(comAddr, lvl);
                    RFSignal.RFPower(comAddr, lvl, 30);
                }

                RFSignal.RFSample(comAddr, lvl);
                RFSignal.RFStart(comAddr);
            }
            return flag;
        }

        #region ���빦�Ŷ�̬�⺯��
        /// <summary>
        /// ���ù���˥��(Att)
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
        /// ���ù�����Դ��
        /// </summary>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFOpenSource")]
        public static extern void RFOpenSource(int priority);

        /// <summary>
        /// ���ù�����Դ��
        /// </summary>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFCloseSource")]
        public static extern void RFCloseSource(int priority);

        /// <summary>
        /// ����ָ�����ȵĹ��Ŷ����б�������Ŀ�괰��������Ϣ��
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
        /// �ͷŹ��Ŷ����б����ر����л�Ĺ����߳�
        /// </summary>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFFinalize")]
        public static extern void RFFinalize();

        /// <summary>
        /// ����Ŷ����б��е�ĳ������
        /// ��������������ַΪComAddr������ΪComName������ΪRFClass�����ʼ��㷽ʽΪRFFormula
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
        /// ��ѯ��ַΪComAddr�Ĺ��ű�����ɹ���TRUE�ɹ���FALSEʧ��
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <returns></returns>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFConnected")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RFConnected(int ComAddr, 
                                              int Timeout);

        /// <summary>
        /// ��ѯ��ַΪComAddr�Ĺ���״̬��Ϣ
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="dest"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStatus")]
        public static extern void RFStatus(int ComAddr, 
                                           ref PowerStatus dest);

        /// <summary>
        /// �����ַΪComAddr���ŵĶ���������б�
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFClear")]
        public static extern void RFClear(int ComAddr, 
                                          int priority);

       /// <summary>
       /// ���õ�ַΪComAddr�Ĺ�������Ƶ�ʣ���λMHz
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
        /// ���õ�ַΪComAddr�Ĺ���������ʣ���λdBm
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        /// <param name="p"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFPower")]
        public static extern void RFPower(int ComAddr, 
                                          int priority, 
                                          float p);

        /// <summary>
        /// ���õ�ַΪComAddr�Ĺ���������ʺ�����Ƶ�ʣ���λdBm��MHz
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
        /// ������ַΪComAddr�Ĺ��ţ����Ž��Ե�ǰ����ֵ����
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFOn")]
        public static extern void RFOn(int ComAddr, 
                                       int priority);

        /// <summary>
        /// �رյ�ַΪComAddr�Ĺ���
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFOff")]
        public static extern void RFOff(int ComAddr, 
                                        int priority);

        /// <summary>
        /// ���õ�ַΪComAddr�Ĺ���������ʺ�����Ƶ�ʣ���λdBm��MHz������������
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
        /// �Ե�ַΪComAddr�Ĺ���ִ�в��ֲ�����ѯ����ȡ���Źؼ���״̬��Ϣ
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention=CallingConvention.StdCall, EntryPoint="RFSample")]
        public static extern void RFSample(int ComAddr,
                                           int priority);

        /// <summary>
        /// �Ե�ַΪComAddr�Ĺ���ִ����ȫ������ѯ����ȡ����ȫ����״̬��Ϣ
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFSample2")]
        public static extern void RFSample2(int ComAddr,
                                           int priority);

        /// <summary>
        /// ����Ե�ַΪComAddr�Ĺ���ִ�з��������
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStart")]
        public static extern void RFStart(int ComAddr);

        /// <summary>
        /// ʹ�������߳���ִ���굱ǰ������󣬾�������������еĺ�������
        /// </summary>
        /// <param name="ComAddr"></param>
        /// <param name="priority"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RFStop")]
        public static extern void RFStop(int ComAddr);

        /// <summary>
        /// ���û�ر���־��¼���ܣ�TRUE ���� FALSE �ر� 
        /// </summary>
        /// <param name="value"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetWndHandle")]
        public static extern void SetWndHandle(IntPtr hwnd);

        /// <summary>
        /// ���û�ر���־��¼���ܣ�TRUE ���� FALSE �ر� 
        /// </summary>
        /// <param name="value"></param>
        [DllImport("jcRFSignal.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "EnableLog")]
        public static extern void EnableLog([MarshalAs(UnmanagedType.Bool)] bool value);
        #endregion
    }

}
