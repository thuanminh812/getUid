using OfficeOpenXml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GetData
{
    public partial class Form1 : Form
    {
        private IWebDriver driver;
        private CancellationTokenSource cts;
        private BindingList<UIDInfo> bindingUIDs = new BindingList<UIDInfo>();
        private int uidCounter = 1;
        private HashSet<string> uniqueUIDs;

        public Form1()
        {
            InitializeComponent();
            uniqueUIDs = new HashSet<string>();
            dataGridView1.DataSource = bindingUIDs;
        }

        private async Task ScanUIDAsync(string groupUrl, CancellationToken cancellationToken)
        {
            try
            {
                driver.Navigate().GoToUrl(groupUrl);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);

                UpdateLabel("Đang lấy danh sách thành viên. Vui lòng đợi...");

                HashSet<string> scannedUIDs = new HashSet<string>();
                int uidCountBeforeScroll = 0;

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                do
                {
                    await LoadMoreData(scannedUIDs, js, cancellationToken);

                    List<IWebElement> memberElements = driver.FindElements(By.CssSelector("a.x1i10hfl.xjbqb8w.x6umtig.x1b1mbwd.xaqea5y.xav7gou.x9f619.x1ypdohk.xt0psk2.xe8uvvx.xdj266r.x11i5rnm.xat24cr.x1mh8g0r.xexx8yu.x4uap5.x18d9i69.xkhd6sd.x16tdsg8.x1hl2dhg.xggy1nq.x1a2a7pz.xt0b8zv.xzsf02u.x1s688f"))
                        .Where(e => !string.IsNullOrEmpty(e.GetAttribute("href")))
                        .ToList();

                    List<string> uids = new List<string>();

                    foreach (IWebElement memberElement in memberElements)
                    {
                        string uid = ExtractUidFromUrl(memberElement.GetAttribute("href"));

                        if (!string.IsNullOrEmpty(uid) && !scannedUIDs.Contains(uid))
                        {
                            uids.Add(uid);

                            UIDInfo uidInfo = new UIDInfo { STT = uidCounter++, UID = uid };
                            UpdateDataGridView(uidInfo);
                            scannedUIDs.Add(uid);
                        }
                    }

                    if (uids.Count <= uidCountBeforeScroll)
                    {
                        break;
                    }

                    uidCountBeforeScroll = uids.Count;

                } while (true);

                UpdateLabel("Đã lấy xong danh sách thành viên!");
                MessageBox.Show("Đã lấy xong danh sách thành viên!");
            }
            catch (OperationCanceledException)
            {
                UpdateLabel("Đã tạm dừng lấy danh sách thành viên!");
                MessageBox.Show("Đã tạm dừng lấy danh sách thành viên!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task LoadMoreData(HashSet<string> scannedUIDs, IJavaScriptExecutor js, CancellationToken cancellationToken)
        {
            try
            {
                long initialHeight = (long)js.ExecuteScript("return document.body.scrollHeight");

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    await Task.Delay(2500);

                    long newHeight = (long)js.ExecuteScript("return document.body.scrollHeight");

                    if (newHeight == initialHeight)
                    {
                        break;
                    }

                    initialHeight = newHeight;

                    List<IWebElement> memberElements = driver.FindElements(By.CssSelector("a.x1i10hfl.xjbqb8w.x6umtig.x1b1mbwd.xaqea5y.xav7gou.x9f619.x1ypdohk.xt0psk2.xe8uvvx.xdj266r.x11i5rnm.xat24cr.x1mh8g0r.xexx8yu.x4uap5.x18d9i69.xkhd6sd.x16tdsg8.x1hl2dhg.xggy1nq.x1a2a7pz.xt0b8zv.xzsf02u.x1s688f"))
                        .Where(e => !string.IsNullOrEmpty(e.GetAttribute("href")))
                        .ToList();

                    foreach (IWebElement memberElement in memberElements)
                    {
                        string href = memberElement.GetAttribute("href");

                        if (href != null)
                        {
                            string uid = ExtractUidFromUrl(href);

                            if (!string.IsNullOrEmpty(uid) && !uniqueUIDs.Contains(uid))
                            {
                                UIDInfo uidInfo = new UIDInfo { STT = uidCounter++, UID = uid };
                                UpdateDataGridView(uidInfo);
                                uniqueUIDs.Add(uid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private static string ExtractUidFromUrl(string url)
        {
            string pattern = @"facebook\.com/groups/\d+/user/(\d+)/";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(url);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private void UpdateDataGridView(UIDInfo data)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.BeginInvoke((MethodInvoker)delegate
                {
                    bindingUIDs.Add(data);
                    dataGridView1.Update();
                });
            }
            else
            {
                bindingUIDs.Add(data);
                dataGridView1.Update();
            }
        }

        private void UpdateLabel(string text)
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke((MethodInvoker)delegate { label1.Text = text; });
            }
            else
            {
                label1.Text = text;
            }
        }
        private async void button4_Click(object sender, EventArgs e)
        {
            try
            {
                string groupUrl = textBox3.Text;

                if (string.IsNullOrEmpty(groupUrl))
                {
                    MessageBox.Show("Vui lòng nhập URL nhóm Facebook.");
                    return;
                }

                label1.Text = "Đang tải...";

                cts = new CancellationTokenSource();

                await Task.Run(() => ScanUIDAsync(groupUrl, cts.Token)); // Chạy tác vụ trên một thread mới
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--disable-notifications");

                options.AddArgument("--user-data-dir=C:\\path\\to\\user\\data\\directory");
                options.AddArgument("--profile-directory=Profile 1");

                driver = new ChromeDriver(options);
                driver.Navigate().GoToUrl("https://www.facebook.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất ra tệp Excel.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("DataSheet");

                List<UIDInfo> uidList = bindingUIDs.ToList();
                DataTable dataTable = ConvertListToDataTable(uidList);

                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Tệp Excel|*.xlsx";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                    excelPackage.SaveAs(excelFile);
                    MessageBox.Show("Xuất dữ liệu thành công!");
                }
            }
        }

        private DataTable ConvertListToDataTable(List<UIDInfo> list)
        {
            DataTable table = new DataTable();
            table.Columns.Add("STT", typeof(int));
            table.Columns.Add("UID", typeof(string));

            foreach (UIDInfo uidInfo in list)
            {
                table.Rows.Add(uidInfo.STT, uidInfo.UID);
            }

            return table;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                label1.Text = "Đang tải...";
                cts = new CancellationTokenSource();

                await Task.Run(() => ScanUIDFriendAsync(cts.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task ScanUIDFriendAsync(CancellationToken cancellationToken)
        {
            try
            {
                driver.Navigate().GoToUrl("https://www.facebook.com/me/friends");

                await Task.Delay(5000);

                string pattern = @"facebook\.com\/profile\.php\?id=(\d+)";
                Regex regex = new Regex(pattern);

                UpdateLabel("Đang lấy danh sách bạn bè. Vui lòng đợi và không sử dụng trang web...");

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");

                    await Task.Delay(6500);

                    IReadOnlyCollection<IWebElement> memberElements = driver.FindElements(By.TagName("a"));

                    bool foundNewUIDs = false;

                    foreach (IWebElement memberElement in memberElements)
                    {
                        if (isPaused)
                        {
                            while (isPaused)
                            {
                                Application.DoEvents();
                            }
                        }

                        string href = memberElement.GetAttribute("href");

                        if (href != null)
                        {
                            Match match = regex.Match(href);

                            if (match.Success)
                            {
                                string uid = match.Groups[1].Value;

                                // Kiểm tra xem UID đã được quét chưa
                                if (!uniqueUIDs.Contains(uid))
                                {
                                    // Nếu chưa quét thì thêm vào danh sách và cập nhật DataGridView
                                    UIDInfo uidInfo = new UIDInfo { STT = uidCounter++, UID = uid };
                                    UpdateDataGridView(uidInfo);
                                    uniqueUIDs.Add(uid); // Thêm UID vào danh sách đã quét
                                    foundNewUIDs = true;
                                }
                            }
                        }
                    }

                    if (!foundNewUIDs)
                    {
                        break;
                    }
                }

                UpdateLabel("Đã lấy xong danh sách bạn bè!");
                MessageBox.Show("Đã lấy xong danh sách bạn bè!");
            }
            catch (OperationCanceledException)
            {
                UpdateLabel("Đã tạm dừng!");
                MessageBox.Show("Đã tạm dừng!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private bool isPaused = false;

        private bool IsUIDAlreadyScanned(string uid)
        {
            return bindingUIDs.Any(uidInfo => uidInfo.UID == uid);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (cts != null && cts.Token.CanBeCanceled)
            {
                isPaused = !isPaused;

                if (isPaused)
                {
                    button5.Text = "Tiếp tục";
                }
                else
                {
                    button5.Text = "Tạm dừng";
                    
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            bindingUIDs.Clear();
            uidCounter = 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cts != null && cts.Token.CanBeCanceled)
            {
                cts.Cancel();
            }

            driver?.Quit();
        }

        public class UIDInfo
        {
            public int STT { get; set; }
            public string UID { get; set; }
        }
    }
}
