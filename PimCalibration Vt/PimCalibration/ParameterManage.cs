using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PimCalibration
{
    class ParameterManage
    {
        //private static string 
        public static TxCalDataStruct tx = new TxCalDataStruct();
        public static RxCalDataStruct rx = new RxCalDataStruct();
        public static ConfigInfo ci = new ConfigInfo();
        public static string txInfo = string.Empty;
        public static string rxInfo = string.Empty;
        public static int INDEX_PIM = 0;
        public static int INDEX_NARROW = 1;
        public static int INDEX_BROAD = 2;

        public static string GetItemString(string str,int item)
        {
            return str.Split(new char[1]{','})[item];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iniPath"></param>
        public static void LoadConfiguration(string iniPath)
        {
            iniPath += @"\configuration.ini";
            IniFile.SetFileName(iniPath);

            try
            {
                ci.formTitle = IniFile.GetString("gloab", "formtitle", string.Empty);
                ci.storePath = IniFile.GetString("gloab", "path", string.Empty);
                string[] str = IniFile.GetString("gloab", "commask", string.Empty).Split(new char[1] { ',' });
                ci.comMask = new string[str.Length];
                ci.comMask = str;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void SaveConfiguration(string iniPath)
        {
            iniPath += @"\configuration.ini";
            IniFile.SetFileName(iniPath);
            IniFile.SetString("gloab", "path",ci.storePath);
        }

        /// <summary>
        /// 读取功放校准信息
        /// </summary>
        /// <param name="iniPath"></param>
        public static void LoadTxChannelPara(string iniPath)
        {
            iniPath += @"\tx_calib.ini";
            IniFile.SetFileName(iniPath);
            string str;

            try
            {
                str = IniFile.GetString("setting", "freq", string.Empty).Replace("mhz", string.Empty);
                tx.freq.AddRange(ArrayString2IntArray(str.Split(new char[1] { ',' })));

                str = IniFile.GetString("setting", "power", string.Empty).Replace("dbm", string.Empty);
                tx.power.AddRange(ArrayString2FloatArray(str.Split(new char[1] { ',' })));

                str = IniFile.GetString("setting", "poweratt", string.Empty).Replace("db", string.Empty);
                tx.powerAtt.AddRange(ArrayString2FloatArray(str.Split(new char[1] { ',' })));

                str = IniFile.GetString("setting", "powerfloat", string.Empty).Replace("db", string.Empty);
                tx.powerFloat.AddRange(ArrayString2FloatArray(str.Split(new char[1] { ',' })));

                tx.SampleCnt = int.Parse(IniFile.GetString("setting", "sampleCount", string.Empty));
                tx.SampleDelay = int.Parse(IniFile.GetString("setting", "sampleDelay", string.Empty));
                tx.SampleOnly = bool.Parse(IniFile.GetString("setting", "sampleOnly", string.Empty));

                tx.PowerOffsetLow = float.Parse(IniFile.GetString("setting", "poweroffsetlow", string.Empty).Replace("db", string.Empty));
                tx.PowerOffsetHigh = float.Parse(IniFile.GetString("setting", "poweroffsethigh", string.Empty).Replace("db", string.Empty));
                tx.PowerOffsetSwitch = float.Parse(IniFile.GetString("setting", "poweroffsetswitch", string.Empty).Replace("dbm", string.Empty));

                tx.PA1Addr = int.Parse(IniFile.GetString("setting", "compa1", string.Empty));
                tx.PA2Addr = int.Parse(IniFile.GetString("setting", "compa2", string.Empty));
                tx.CycleCnt = int.Parse(IniFile.GetString("setting", "cycleCnt", string.Empty));
                tx.CalDelay = int.Parse(IniFile.GetString("setting", "CalDelay", string.Empty));
                tx.PAformule = int.Parse(IniFile.GetString("setting", "PAformule", string.Empty));
                tx.Step = float.Parse(IniFile.GetString("setting", "step", string.Empty));
                tx.InsType = IniFile.GetString("setting", "device", string.Empty);
                tx.PAType = IniFile.GetString("setting", "patype", string.Empty);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //ci.comMask = new string[] { "COM" + tx.PA1Addr.ToString(), "COM" + tx.PA2Addr.ToString() };

            txInfo = "<Power Meter>: " + tx.InsType + "\r\n" +
                           "<PA Type>: " + tx.PAType + "\r\n" +
                           "<Calibration Power Point Count>:" + tx.power.Count.ToString() + "\r\n";                       

            txInfo +="<Powe Table>:\r\n";

            for (int i = 0; i < tx.power.Count; i++)
            {
                txInfo += tx.power[i].ToString() + "dBm ";
            }

            txInfo += "<Calibration Freq Point Count>:" + tx.freq.Count.ToString() + "\r\n";
            txInfo += "<Frequcene Table>:\r\n";

            for (int i = 0; i < tx.freq.Count; i++)
            {
                txInfo += tx.freq[i].ToString() + "MHz ";
            }
        }

        public static void LoadTxCalibPara(string iniPath)
        {
            iniPath += @"\Tx_Tables";
            if (Directory.Exists(iniPath) == false)
                Directory.CreateDirectory(iniPath);
            iniPath += @"\signal_tx.ini";
            IniFile.SetFileName(iniPath);
            
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < tx.power.Count; j++)
                {
                    string str = IniFile.GetString("pim_signal_" + (i + 1).ToString(), "tx_row_"+(j+1).ToString(), string.Empty);
                    float[] ft = ArrayString2FloatArray(str.Split(new char[1] { ',' }));
                    for (int k = 0; k < ft.Length; k++)
                    {
                        tx.powerCalib[i, k, j] = ft[k];
                    }
                }
            }
        }

        private static int[] ArrayString2IntArray(string[] str)
        {
            List<int > lt = new List<int>();

            for (int i = 0; i < str.Length; i++)
            {
                lt.Add(int.Parse(str[i]));
            }

            return lt.ToArray();
        }

        private static float[] ArrayString2FloatArray(string[] str)
        {
            List<float> lt = new List<float>();

            for (int i = 0; i < str.Length; i++)
            {
                lt.Add(float.Parse(str[i]));
            }

            return lt.ToArray();
        }

        private static string ArrayInt2String(int[] val)
        {
            string str = string.Empty;

            for (int i = 0; i < val.Length; i++)
            {
                str += val[i].ToString();
                if (i != val.Length - 1)
                    str += ",";
            }
            return str;
        }

        private static string ArrayFloat2String(float[] val)
        {
            string str = string.Empty;

            for (int i = 0; i < val.Length; i++)
            {
                str += val[i].ToString("F2");
                if (i != val.Length - 1)
                    str += ",";
            }
            return str;
        }

        private static void LoadRxChannelParaSub(int item,string name)
        {
            try
            {
                string str;

                rx.channel[item].start = int.Parse(IniFile.GetString(name, "start", string.Empty).Replace("khz", string.Empty));
                rx.channel[item].stop = int.Parse(IniFile.GetString(name, "stop", string.Empty).Replace("khz", string.Empty));
                rx.channel[item].vbw = int.Parse(IniFile.GetString(name, "vbw", string.Empty).Replace("khz", string.Empty));
                rx.channel[item].att = int.Parse(IniFile.GetString(name, "att", string.Empty).Replace("db", string.Empty));

                str = IniFile.GetString(name, "rbw", string.Empty).Replace("khz", string.Empty);
                rx.channel[item].rbw.AddRange(ArrayString2IntArray(str.Split(new char[1] { ',' })));
                str = IniFile.GetString(name, "power", string.Empty).Replace("dbm", string.Empty);
                rx.channel[item].power.AddRange(ArrayString2FloatArray(str.Split(new char[1] { ',' })));
                str = IniFile.GetString(name, "freq", string.Empty).Replace("mhz", string.Empty);
                rx.channel[item].freq.AddRange(ArrayString2IntArray(str.Split(new char[1] { ',' })));
                str = IniFile.GetString(name, "span", string.Empty);
                rx.channel[item].span.AddRange(ArrayString2IntArray(str.Split(new char[1] { ',' })));
            }
            catch(Exception ex)
            {
                throw ex;
            }

            //for (int i = 0; i < rx.channel[item].rbw.Count; i++)
            //{
            //    rx.channel[item].powerCal.Add(0);
            //}
        }

        /// <summary>
        /// 导入Rx参数列表
        /// </summary>
        /// <param name="iniPath"></param>
        /// <returns></returns>
        public static bool LoadRxChannelPara(string iniPath)
        { 
            //StringBuilder sb = new StringBuilder(256);
            iniPath += @"\rx_calib.ini";
            IniFile.SetFileName(iniPath);

            rx.insType = IniFile.GetString("parameter", "insType", string.Empty);
            rx.specType = IniFile.GetString("parameter", "specType", string.Empty);
            rx.startIdx = int.Parse(IniFile.GetString("parameter", "startIdx", string.Empty));

            LoadRxChannelParaSub(ParameterManage.INDEX_PIM,"pim");
            LoadRxChannelParaSub(ParameterManage.INDEX_NARROW,"narrow");
            LoadRxChannelParaSub(ParameterManage.INDEX_BROAD,"broad");

            rxInfo = "<Signal Generator>: " + rx.insType + "\r\n" +
                           "<Sepctrum>: " + rx.specType + "\r\n" +                         
                           "<Table>:\r\n"+
                           "|=Narrow               |=Board\r\n";

            //int tabCnt = (rx.channel[1].power.Count - rx.channel[2].power.Count)>=0 ? rx.channel[1].power.Count : rx.channel[2].power.Count;

            //for (int i = 0; i < tabCnt; i++)
            //{
            //    if (i < rx.channel[1].power.Count)
            //    {
            //        rxInfo += rx.channel[1].freq[i].ToString() + "MHz, " + rx.channel[1].power[i].ToString() + "dBm; ";
            //    }

            //    if (i < rx.channel[2].power.Count)
            //    {
            //        rxInfo += rx.channel[2].freq[i].ToString() + "MHz, " + rx.channel[2].power[i].ToString() + "dBm";
            //    }

            //    rxInfo += "\r\n";
            //}

            return false;
        }      

        public static bool SaveTxChannelPara(string iniPath)
        {
            if (ci.storePath != string.Empty)
                iniPath = ci.storePath;

            iniPath += @"\Tx_Tables";
            Directory.CreateDirectory(iniPath);

            IniFile.SetFileName(iniPath + @"\signal_tx_disp.ini");
            //pim_signal_1,pim_signal_2
            for (int i = 1; i < 3; i++)
            {
                IniFile.SetString("pim_signal_" + i.ToString(), "tx_p", ArrayFloat2String(tx.power.ToArray()));
                IniFile.SetString("pim_signal_" + i.ToString(), "tx_f", ArrayInt2String(tx.freq.ToArray()));

                IniFile.SetString("pim_offset_" + i.ToString(), "tx_p", ArrayFloat2String(tx.power.ToArray()));
                IniFile.SetString("pim_offset_" + i.ToString(), "tx_f", ArrayInt2String(tx.freq.ToArray()));

                for (int j = 0; j < tx.power.Count; j++)
                {
                    float[] pr = new float[tx.freq.Count];

                    for (int k = 0; k < tx.freq.Count; k++)
                    {
                        pr[k] = tx.powerDisp[i - 1, k, j];
                    }

                    IniFile.SetString("pim_signal_" + i.ToString(), "tx_row_" + (j + 1).ToString(), ArrayFloat2String(pr));
                    IniFile.SetString("pim_offset_" + i.ToString(), "tx_row_" + (j + 1).ToString(), "0");
                }
            }

            if (tx.SampleOnly) return true;

            IniFile.SetFileName(iniPath + @"\signal_tx.ini");
            //pim_signal_1,pim_signal_2
            for (int i = 1; i < 3; i++)
			{
                IniFile.SetString("pim_signal_" + i.ToString(), "tx_p",ArrayFloat2String(tx.power.ToArray()));
                IniFile.SetString("pim_signal_" + i.ToString(), "tx_f",ArrayInt2String(tx.freq.ToArray()));

                IniFile.SetString("pim_offset_" + i.ToString(), "tx_p", ArrayFloat2String(tx.power.ToArray()));
                IniFile.SetString("pim_offset_" + i.ToString(), "tx_f", ArrayInt2String(tx.freq.ToArray()));

                for (int j = 0; j < tx.power.Count; j++)
                {
                    float[] pr = new float[tx.freq.Count];

                    for (int k = 0; k < tx.freq.Count; k++)
                    {
                        pr[k] = tx.powerCalib[i - 1, k, j];
                    }

                    IniFile.SetString("pim_signal_" + i.ToString(), "tx_row_" + (j+1).ToString(), ArrayFloat2String(pr));
                    IniFile.SetString("pim_offset_" + i.ToString(), "tx_row_" + (j+1).ToString(), "0");
                }
			}

            return true;
        }

        public static bool SaveRxChannelPara(string iniPath)
        {
            if (ci.storePath != string.Empty)
                iniPath = ci.storePath;

            string pathSpec = iniPath + @"\Spectrum_Tables";
            string pathPim = iniPath + @"\Rx_Tables";
            Directory.CreateDirectory(pathSpec);
            Directory.CreateDirectory(pathPim);

            int[] rbwName = new int[] { 4,20,100,1000};

            //频谱仪各个RBW的校准文件
            for (int i = 1; i < rx.channel.Length; i++)
            {
                RxCalDataStruct.bandChannel chanSpec = rx.channel[i];
                for (int j = 0; j < chanSpec.rbw.Count; j++)
                {
                    //保存文件                    
                    StreamWriter sw = File.CreateText(pathSpec + @"\CH" + i.ToString() + "_" + rbwName[j].ToString() + "KHz.txt");
                    for (int k = 0; k < chanSpec.freq.Count; k++)
                    {
                        sw.WriteLine(chanSpec.freq[k].ToString() + "," + chanSpec.powerCal[j,k].ToString("F2"));
                    }
                    sw.Dispose();
                }
            }

            //保存互调的校准文件
            RxCalDataStruct.bandChannel chanPim = rx.channel[ParameterManage.INDEX_PIM];
            //保存文件
            StreamWriter swpim = File.CreateText(pathPim + @"\pim.txt");
            for (int i = 0; i < chanPim.freq.Count; i++)
            {
                swpim.WriteLine(chanPim.freq[i].ToString() + "," + chanPim.powerCal[0, i].ToString("F2"));               
            }
            swpim.Dispose();

            return true;
        }

        public static bool SaveTxFailedData(string iniPath)
        {
            if (ci.storePath != string.Empty)
                iniPath = ci.storePath;

            iniPath += @"\CalibFailed";
            Directory.CreateDirectory(iniPath);

            iniPath += @"\CalibFailed.txt";

            if (File.Exists(iniPath))
            {
                File.Delete(iniPath);
            }

            StreamWriter sw = File.CreateText(iniPath);

            sw.WriteLine("//以下是未校准的功率点，注意每一行描述了一个点的信息。");

            foreach( float freq in tx.errCollect1.Keys )
            {
                sw.WriteLine("==>PA1  FREQ: " + freq.ToString() + "  POWER:"+ tx.errCollect1[freq].ToString());            
            }

            foreach (float freq in tx.errCollect2.Keys)
            {
                sw.WriteLine("==>PA2  FREQ: " + freq.ToString() + "  POWER:" + tx.errCollect2[freq].ToString());
            }

            sw.Dispose();

            System.Diagnostics.Process.Start("notepad.exe", iniPath);

            return true;
        }
    }
}
