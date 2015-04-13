using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace PimCalibration
{
    class IniFile
    {
        private static string fName = "";

        private static readonly uint maxCharCount = 256;

        // DWORD WINAPI GetPrivateProfileString(
        // __in   LPCTSTR lpAppName,
        // __in   LPCTSTR lpKeyName,
        // __in   LPCTSTR lpDefault,
        // __out  LPTSTR lpReturnedString,
        // __in   DWORD nSize,
        // __in   LPCTSTR lpFileName
        // );
        [DllImport("Kernel32.dll", EntryPoint="GetPrivateProfileStringA")]
        private static extern uint GetPrivateProfileStringA( [In()] [MarshalAs(UnmanagedType.LPStr)] string sectionName,
                                                             [In()] [MarshalAs(UnmanagedType.LPStr)] string keyName,
                                                             [In()] [MarshalAs(UnmanagedType.LPStr)] string defaultString,         
                                                             [Out()] [MarshalAs(UnmanagedType.LPStr)] StringBuilder returnedString,
                                                             uint charCount,
                                                             [In()] [MarshalAs(UnmanagedType.LPStr)] string fName);
                
        //  BOOL WINAPI WritePrivateProfileString(
        //  __in  LPCTSTR lpAppName,
        //  __in  LPCTSTR lpKeyName,
        //  __in  LPCTSTR lpString,
        //  __in  LPCTSTR lpFileName
        //  );
        [DllImport("Kernel32.dll",  EntryPoint="WritePrivateProfileStringA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileStringA( [In()] [MarshalAs(UnmanagedType.LPStr)] string sectionName,
                                                               [In()] [MarshalAs(UnmanagedType.LPStr)] string keyName,
                                                               [In()] [MarshalAs(UnmanagedType.LPStr)] string value,
                                                               [In()] [MarshalAs(UnmanagedType.LPStr)] string fName);



        internal static string GetString(string section,
                                         string key,
                                         string defaultValue)
        {
            StringBuilder sb = new StringBuilder((int)maxCharCount);

            GetPrivateProfileStringA(section, key, defaultValue, sb, maxCharCount, fName);

            return sb.ToString();
        }

        internal static bool SetString(string section,
                                       string key,
                                       string value)
        {
           return WritePrivateProfileStringA(section, key, value, fName);
        }

        internal static void SetFileName(string fileName)
        {
            fName = fileName;
        }
   
        /// <summary>
        /// 从以逗号和空格隔开的字符串str
        /// 获取第i项，最多maxCount项，索引从零开始
        /// </summary>
        /// <param name="str"></param>
        /// <param name="i"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        internal static string GetItemFrom(string str, int i, int maxCount)
        {
            int j1, j2, k;
            string item = "";

            //获取最后一项
            if (i >= (maxCount - 1))
            {
                j1 = str.LastIndexOf(",");

                if (j1 >= 0)
                    item = str.Substring((j1+1), (str.Length - j1 - 1));

            //获取前面的项
            } else {
                k = 0;
                j1 = 0;
                j2 = str.IndexOf(',', j1);
                if (j2 < 0)
                    item = str;
                
                while (j2 > 0)
                {
                    k++;

                    if (k >= (i + 1))
                        break;

                    j1 = j2;

                    j2 = str.IndexOf(',', (j1 + 1));
                }

                if (k == (i + 1))
                {
                    if (j1 > 0)
                        item = str.Substring((j1+1), (j2 - j1 - 1));
                    else
                        item = str.Substring(j1, (j2 - j1));
                }
            }

            //返回找到的项
            return item.Trim();
        }


        /// <summary>
        /// 在以逗号和空格隔开的字符串, 获知其包含数据项的数目
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static int CountOfItemIn(string str)
        {
            int c  = 0;
            int i1 = 0;
            int i2 = 0;
            string item = "";
         
            i2 = str.IndexOf(',', i1);

            while (i2 > 0)
            {
                c++;              

                i1 = i2;

                i2 = str.IndexOf(',', (i1 + 1));
            }

            if (c > 0)
            {
                i1 = str.LastIndexOf(",");

                item = str.Substring(i1, (str.Length - i1));

                if (!String.IsNullOrEmpty(item.Trim()))
                    c++;
            }

            return c;
        }


    }
}
