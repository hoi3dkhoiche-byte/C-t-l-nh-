using System.Drawing;
using System.Windows.Forms;

namespace ClientAPP;

public class BaseForm : Form
{
    public BaseForm()
    {
        this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(244, 246, 249);
    }

    protected void ShowLoading(bool isLoading)
    {
        Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
    }

    protected void ShowError(string message, string title = "Lỗi")
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    protected void ShowSuccess(string message, string title = "Thành công")
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected void ShowInfo(string message, string title = "Thông báo")
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected bool Confirm(string question, string title = "Xác nhận")
    {
        return MessageBox.Show(this, question, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected()
    {
        ShowError("Mất kết nối tới máy chủ.");
    }
}
