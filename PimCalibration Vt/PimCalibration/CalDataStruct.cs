using System;
using System.Collections.Generic;
using System.Text;

namespace PimCalibration
{
    /// <summary>
    /// 配置信息
    /// </summary>
    class ConfigInfo
    {
        public string formTitle;
        public string[] comMask;
        /// <summary>
        /// 储存路径
        /// </summary>
        public string storePath;
    }
    /// <summary>
    /// 功放校准数据类
    /// </summary>
    class TxCalDataStruct
    {
        public enum EPowerDivide
        {
            low,high
        }
        /// <summary>
        /// 功率计型号
        /// </summary>
        public string InsType;
        public string InsCom;
        public string PAType;
        public int RFPriority;
        /// <summary>
        /// 校准步进
        /// </summary>
        public float Step;
        /// <summary>
        /// 功放性质
        /// </summary>
        public int PAformule;
        /// <summary>
        /// 校准延时
        /// </summary>
        public int CalDelay;
        /// <summary>
        /// 采样延时
        /// </summary>
        public int SampleDelay;
        /// <summary>
        /// 采样计数
        /// </summary>
        public int SampleCnt;
        /// <summary>
        /// 
        /// </summary>
        public bool SampleOnly;
        /// <summary>
        /// 循环次数
        /// </summary>
        public int CycleCnt;
        /// <summary>
        /// 失败次数[TKey,TValue] = [频率，功率]
        /// </summary>
        public Dictionary<float, float> errCollect1 = new Dictionary<float, float>();
        /// <summary>
        /// 失败次数[TKey,TValue] = [频率，功率]
        /// </summary>
        public Dictionary<float, float> errCollect2 = new Dictionary<float, float>();
        /// <summary>
        /// 功放1串口地址
        /// </summary>
        public int PA1Addr;
        /// <summary>
        /// 功放2串口地址
        /// </summary>
        public int PA2Addr;
        /// <summary>
        /// 耦合器插损1
        /// </summary>
        public float PowerOffsetLow;
        /// <summary>
        /// 耦合器插损2
        /// </summary>
        public float PowerOffsetHigh;
        /// <summary>
        /// 更改耦合的功率分界值
        /// </summary>
        public float PowerOffsetSwitch;
        /// <summary>
        /// 功率表
        /// </summary>
        public List<float> power = new List<float>();
        /// <summary>
        /// 衰减表
        /// </summary>
        public List<float> powerAtt = new List<float>();
        /// <summary>
        /// 功率抖动范围
        /// </summary>
        public List<float> powerFloat = new List<float>();
        /// <summary>
        /// 频率表
        /// </summary>
        public List<int> freq = new List<int>();
        /// <summary>
        /// 输出校准表[PANum,  freq,  power] 对应功率表和频率表 
        /// </summary>
        public float[, ,] powerCalib = new float[2, 50, 50];
        /// <summary>
        /// 显示校准表[PANum,  freq,  power] 对应功率表和频率表 
        /// </summary>
        public float[, ,] powerDisp = new float[2, 50, 50];    
        //public List<List<float>> calTable = new List<>();
    }

    /// <summary>
    /// 接收通道校准类
    /// </summary>
    class RxCalDataStruct
    {
        public class bandChannel
        {
            /// <summary>
            /// 起始频率
            /// </summary>
            public int start;
            /// <summary>
            /// 终止频率
            /// </summary>
            public int stop;
            /// <summary>
            /// 衰减值
            /// </summary>
            public int att;
            /// <summary>
            /// 视频带宽
            /// </summary>
            public int vbw;
            /// <summary>
            /// dbm
            /// </summary>
            public List<float> power = new List<float>();
            /// <summary>
            /// 实际校准值
            /// </summary>
            //public List<float> powerCal = new List<float>();
            public float[,] powerCal = new float[100, 100];
            /// <summary>
            /// MHz
            /// </summary>
            public List<int> freq = new List<int>();
            /// <summary>
            /// KHz
            /// </summary>
            public List<int> rbw = new List<int>();
            /// <summary>
            /// ?
            /// </summary>
            public List<int> span = new List<int>();           
        }
        /// <summary>
        /// 信号发生器型号
        /// </summary>
        public string insType;
        public string insCom;
        public string specType;
        public int startIdx;
        /// <summary>
        /// channel[0]为PIM,channel[1]为窄带，channel[2]为宽带
        /// </summary>
        public bandChannel[] channel = new bandChannel[]{new bandChannel(),
                                                                                             new bandChannel(),
                                                                                             new bandChannel(),};
    }
}
