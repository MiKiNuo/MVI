namespace Winform;

partial class LoginView
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        _sureBtn = new System.Windows.Forms.Button();
        _cancleBtn = new System.Windows.Forms.Button();
        _nameTxt = new System.Windows.Forms.TextBox();
        label1 = new System.Windows.Forms.Label();
        _pwdTxt = new System.Windows.Forms.TextBox();
        label2 = new System.Windows.Forms.Label();
        _infoLbl = new System.Windows.Forms.Label();
        SuspendLayout();
        // 
        // _sureBtn
        // 
        _sureBtn.Location = new System.Drawing.Point(69, 134);
        _sureBtn.Name = "_sureBtn";
        _sureBtn.Size = new System.Drawing.Size(75, 23);
        _sureBtn.TabIndex = 0;
        _sureBtn.Text = "确定";
        _sureBtn.UseVisualStyleBackColor = true;
        // 
        // _cancleBtn
        // 
        _cancleBtn.Location = new System.Drawing.Point(160, 134);
        _cancleBtn.Name = "_cancleBtn";
        _cancleBtn.Size = new System.Drawing.Size(75, 23);
        _cancleBtn.TabIndex = 1;
        _cancleBtn.Text = "取消";
        _cancleBtn.UseVisualStyleBackColor = true;
        // 
        // _nameTxt
        // 
        _nameTxt.Location = new System.Drawing.Point(89, 20);
        _nameTxt.Name = "_nameTxt";
        _nameTxt.Size = new System.Drawing.Size(100, 23);
        _nameTxt.TabIndex = 2;
        // 
        // label1
        // 
        label1.Location = new System.Drawing.Point(45, 23);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(38, 23);
        label1.TabIndex = 3;
        label1.Text = "姓名";
        // 
        // _pwdTxt
        // 
        _pwdTxt.Location = new System.Drawing.Point(89, 77);
        _pwdTxt.Name = "_pwdTxt";
        _pwdTxt.Size = new System.Drawing.Size(100, 23);
        _pwdTxt.TabIndex = 4;
        // 
        // label2
        // 
        label2.Location = new System.Drawing.Point(45, 80);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(38, 23);
        label2.TabIndex = 5;
        label2.Text = "密码";
        // 
        // _infoLbl
        // 
        _infoLbl.Location = new System.Drawing.Point(69, 188);
        _infoLbl.Name = "_infoLbl";
        _infoLbl.Size = new System.Drawing.Size(177, 98);
        _infoLbl.TabIndex = 6;
        _infoLbl.Text = "label3";
        // 
        // LoginView
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(291, 295);
        Controls.Add(_infoLbl);
        Controls.Add(label2);
        Controls.Add(_pwdTxt);
        Controls.Add(label1);
        Controls.Add(_nameTxt);
        Controls.Add(_cancleBtn);
        Controls.Add(_sureBtn);
        MaximizeBox = false;
        Text = "登录";
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Label _infoLbl;

    private System.Windows.Forms.TextBox _nameTxt;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox _pwdTxt;
    private System.Windows.Forms.Label label2;

    private System.Windows.Forms.Button _cancleBtn;

    private System.Windows.Forms.Button _sureBtn;

    #endregion
}