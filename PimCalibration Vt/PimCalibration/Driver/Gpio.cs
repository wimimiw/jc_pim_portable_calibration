using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace PimCalibration
{
    public class Gpio
    {
        /// <summary>打开GPIO设备
        /// 
        /// </summary>
        /// <returns>返回：如果设备打开成功则返回设备号，否则返回-1。</returns>
        [DllImport("GPIO_E6")]
        public static extern int GPIO_Open();

        /// <summary>关闭GPIO
        /// 
        /// </summary>
        /// <param name="hDevice"></param>
        /// <returns>返回：如果关闭成功返回true,否则返回false.</returns>
        [DllImport("GPIO_E6")]
        public static extern bool GPIO_Close(int hDevice);

        /// <summary>读取GPIO指定PORT的数据
        /// 
        /// </summary>
        /// <param name="hDevice">输入参数：hDevice： 由GPIO_Open 打开的设备符号</param>
        /// <param name="portnum">portnum： GPIO指定PORT</param>
        /// <returns>返回：如果读取数据成功返回GPIO端口的数值（0或者1，指的是置低或者置高），读取不成功返回-1.</returns>
        [DllImport("GPIO_E6")]
        public static extern int GPIO_Get(int hDevice, int portnum);

        /// <summary>输出数据到GPIO指定PORT
        /// 
        /// </summary>
        /// <param name="hDevice"> 输入参数：hDevice： 由GPIO_Open 打开的设备符号；</param>
        /// <param name="portnum">portnum： GPIO指定PORT </param>
        /// <param name="portval">portval：指定的数值（0或者1，指的是置低或者置高）</param>
        /// <returns>返回：如果输出数据成功返回true,否则返回false.</returns>
        [DllImport("GPIO_E6")]
        public static extern bool GPIO_Set(int hDevice, int portnum, int portval);

    }
}
