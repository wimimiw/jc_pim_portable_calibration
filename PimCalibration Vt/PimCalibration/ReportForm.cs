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
            int sum = ParameterManage.tx.freq.Count*ParameterManage.tx.power.Count;
            int noFinish = ParameterManage.tx.errCollect1.Count;
            this.label1.Text += noFinish.ToString();
            this.label2.Text += (sum - noFinish).ToString();
            this.label3.Text += sum.ToString();

            if (noFinish == 0)
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
