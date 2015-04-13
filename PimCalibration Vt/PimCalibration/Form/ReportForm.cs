using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PimCalibration
{
    public partial class ReportForm : Form
    {
        public ReportForm()
        {
            InitializeComponent();
        }

        private void ReportForm_Load(object sender, EventArgs e)
        {
            int sum1 = ParameterManage.tx.PA[0].freq.Count * ParameterManage.tx.PA[0].power.Count;
            int sum2 = ParameterManage.tx.PA[1].freq.Count * ParameterManage.tx.PA[1].power.Count;                     
            int[] noFinish = new int[]{0,0};

            for (int i = 0; i < ParameterManage.tx.PA.Length; i++)
            {
                for (int j = 0; j < ParameterManage.tx.PA[i].freq.Count; j++)
                {
                    for (int k = 0; k < ParameterManage.tx.PA[i].power.Count; k++)
                    {
                        if (ParameterManage.tx.errCollect[i, j, k])
                            noFinish[i]++;
                    }
                }
            }

            this.label1.Text += noFinish[0].ToString();
            this.label2.Text += (sum1 - noFinish[0]).ToString();
            this.label3.Text += noFinish[1].ToString();
            this.label4.Text += (sum2 - noFinish[1]).ToString();

            if (noFinish[0] == 0 && noFinish[1] == 0)
            {
                this.button1.Text = "保存并退出";
            }
            else
            {
                this.button1.Text = "保存并打开未校准点数文件";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
