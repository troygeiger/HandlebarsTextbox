namespace HandlebarsTextbox.Winforms.Test;

partial class Form1
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        handlebarTextbox1 = new HandlebarTextbox();
        SuspendLayout();
        // 
        // handlebarTextbox1
        // 
        handlebarTextbox1.Location = new Point(58, 24);
        handlebarTextbox1.Name = "handlebarTextbox1";
        handlebarTextbox1.Size = new Size(480, 23);
        handlebarTextbox1.TabIndex = 0;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(handlebarTextbox1);
        Name = "Form1";
        Text = "Form1";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private HandlebarsTextbox.Winforms.HandlebarTextbox handlebarTextbox1;
}
