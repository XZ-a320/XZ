using System;
using System.Drawing;
using System.Windows.Forms;

namespace Checklist320
{
    // 检查项实体
    public class CheckItem
    {
        public string Description { get; set; } = "";
        public bool IsImportant { get; set; }
        public bool IsChecked { get; set; }
    }

    public partial class MainForm : Form
    {
        private TabControl tabControl;

        public MainForm()
        {
            this.Text = "A320 检查单";
            this.Size = new Size(320, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tabControl = new TabControl { Dock = DockStyle.Fill };

            // 添加各个阶段选项卡
            AddTabPage("地面操作", CreateGroundChecklist());
            AddTabPage("起飞前", CreateBeforeTakeoffChecklist());
            AddTabPage("滑行/起飞后", CreateAfterTakeoffChecklist());
            AddTabPage("巡航", CreateCruiseChecklist());
            AddTabPage("降落", CreateLandingChecklist());
            AddTabPage("紧急情况", CreateEmergencyChecklist());

            this.Controls.Add(tabControl);
        }

        private void AddTabPage(string title, CheckItem[] items)
        {
            var page = new TabPage(title);
            page.AutoScroll = true;
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(5)
            };

            foreach (var item in items)
            {
                var chk = new CheckBox
                {
                    Text = item.Description,
                    AutoSize = true,
                    Checked = item.IsChecked,
                    Margin = new Padding(0, 3, 0, 3)
                };

                if (item.IsImportant)
                {
                    chk.ForeColor = Color.Red;
                    chk.Font = new Font(chk.Font, FontStyle.Bold);
                }

                // 实时保存勾选状态（可选）
                chk.CheckedChanged += (s, e) => item.IsChecked = chk.Checked;

                panel.Controls.Add(chk);
            }

            page.Controls.Add(panel);
            tabControl.TabPages.Add(page);
        }

        #region 检查项定义（可根据需要修改）
        private CheckItem[] CreateGroundChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "检查飞机外部无损伤", IsImportant = true },
                new() { Description = "检查无油液泄露", IsImportant = true },
                new() { Description = "检查燃油量符合计划", IsImportant = false },
                new() { Description = "登机桥/梯已撤离", IsImportant = false },
                new() { Description = "舱门关闭并锁定", IsImportant = true },
                new() { Description = "轮挡已移除", IsImportant = true }
            };
        }

        private CheckItem[] CreateBeforeTakeoffChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "起飞构型测试完成", IsImportant = true },
                new() { Description = "起飞重量/速度计算", IsImportant = true },
                new() { Description = "襟翼设置到起飞位置", IsImportant = true },
                new() { Description = "配平设置正确", IsImportant = true },
                new() { Description = "飞行指引仪接通", IsImportant = false },
                new() { Description = "航路点确认", IsImportant = false },
                new() { Description = "空管许可已获得", IsImportant = true },
                new() { Description = "应答机设置正确", IsImportant = false },
                new() { Description = "防撞灯开启", IsImportant = true }
            };
        }

        private CheckItem[] CreateAfterTakeoffChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "起落架收起并检查", IsImportant = true },
                new() { Description = "襟翼按计划收上", IsImportant = true },
                new() { Description = "发动机推力设置", IsImportant = false },
                new() { Description = "增压系统正常", IsImportant = false },
                new() { Description = "过渡高度层设置", IsImportant = true }
            };
        }

        private CheckItem[] CreateCruiseChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "燃油平衡检查", IsImportant = true },
                new() { Description = "发动机参数正常", IsImportant = true },
                new() { Description = "气象雷达监控", IsImportant = false },
                new() { Description = "客舱温度/增压正常", IsImportant = false }
            };
        }

        private CheckItem[] CreateLandingChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "着陆重量/速度计算", IsImportant = false },
                new() { Description = "襟翼设置着陆构型", IsImportant = true },
                new() { Description = "起落架放下并锁定", IsImportant = true },
                new() { Description = "减速板预位", IsImportant = true },
                new() { Description = "自动刹车设置", IsImportant = false },
                new() { Description = "空管落地许可", IsImportant = true },
                new() { Description = "应答机模式正确", IsImportant = false }
            };
        }

        private CheckItem[] CreateEmergencyChecklist()
        {
            return new CheckItem[]
            {
                new() { Description = "紧急下降", IsImportant = false },
                new() { Description = "执行记忆项目", IsImportant = true },
                new() { Description = "应急代码 7700 挂出", IsImportant = true },
                new() { Description = "通知空管紧急状态", IsImportant = true },
                new() { Description = "确认安全并疏散", IsImportant = false }
            };
        }
        #endregion

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}