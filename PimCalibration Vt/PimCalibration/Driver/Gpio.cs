using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace PimCalibration
{
    public class Gpio
    {
        /// <summary>��GPIO�豸
        /// 
        /// </summary>
        /// <returns>���أ�����豸�򿪳ɹ��򷵻��豸�ţ����򷵻�-1��</returns>
        [DllImport("GPIO_E6")]
        public static extern int GPIO_Open();

        /// <summary>�ر�GPIO
        /// 
        /// </summary>
        /// <param name="hDevice"></param>
        /// <returns>���أ�����رճɹ�����true,���򷵻�false.</returns>
        [DllImport("GPIO_E6")]
        public static extern bool GPIO_Close(int hDevice);

        /// <summary>��ȡGPIOָ��PORT������
        /// 
        /// </summary>
        /// <param name="hDevice">���������hDevice�� ��GPIO_Open �򿪵��豸����</param>
        /// <param name="portnum">portnum�� GPIOָ��PORT</param>
        /// <returns>���أ������ȡ���ݳɹ�����GPIO�˿ڵ���ֵ��0����1��ָ�����õͻ����øߣ�����ȡ���ɹ�����-1.</returns>
        [DllImport("GPIO_E6")]
        public static extern int GPIO_Get(int hDevice, int portnum);

        /// <summary>������ݵ�GPIOָ��PORT
        /// 
        /// </summary>
        /// <param name="hDevice"> ���������hDevice�� ��GPIO_Open �򿪵��豸���ţ�</param>
        /// <param name="portnum">portnum�� GPIOָ��PORT </param>
        /// <param name="portval">portval��ָ������ֵ��0����1��ָ�����õͻ����øߣ�</param>
        /// <returns>���أ����������ݳɹ�����true,���򷵻�false.</returns>
        [DllImport("GPIO_E6")]
        public static extern bool GPIO_Set(int hDevice, int portnum, int portval);

    }
}
