using System;
using System.Collections.Generic;
using System.Text;

namespace PimCalibration
{
    internal class MessageID
    {      
        public const int RF_SUCCED_ALL = 0X0400 + 1000;
        public const int RF_SUCCED_ONE = 0X0400 + 1001;
        public const int RF_FAILED     = 0X0400 + 1002;
        public const int RF_ERROR      = 0X0400 + 1003;

        public const int RF_VSWR_WARNINIG = 0X0400 + 1028;
        
        public const int SPECTRUEME_SUCCED = 0X0400 + 1004;      

        public const int SPECTRUM_ERROR      = 0X0400 + 1006;

        public const int PIM_SUCCED = 0X0400 + 1007;
        public const int ISO_SUCCED = 0X0400 + 1008;
        public const int VSW_SUCCED = 0X0400 + 1009;
        public const int HAR_SUCCED = 0X0400 + 1010;

        public const int PIM_SWEEP_DONE = 0X0400 + 1011;
        public const int ISO_SWEEP_DONE = 0X0400 + 1012;
        public const int VSW_SWEEP_DONE = 0X0400 + 1013;
        public const int HAR_SWEEP_DONE = 0X0400 + 1014;
        public const int SF_WAIT = 0X0400 + 1020;
        public const int SF_CONTINUTE = 0X0400 + 1021;

        public const int PIM_SWEEP_CLOSE = 0X0400 + 1027;


        private MessageID()
        {
            //
        }
    }
}
