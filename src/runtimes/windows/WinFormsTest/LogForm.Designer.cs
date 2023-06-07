/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

namespace WinFormsTest
{
    partial class LogForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
      this.btnInfo = new System.Windows.Forms.Button();
      this.tbFrom = new System.Windows.Forms.TextBox();
      this.tbText = new System.Windows.Forms.TextBox();
      this.btnWarning = new System.Windows.Forms.Button();
      this.btnError = new System.Windows.Forms.Button();
      this.btnCatastrophy = new System.Windows.Forms.Button();
      this.btnDebug = new System.Windows.Forms.Button();
      this.btnTrace = new System.Windows.Forms.Button();
      this.btnWebCall = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // btnInfo
      // 
      this.btnInfo.Location = new System.Drawing.Point(21, 155);
      this.btnInfo.Name = "btnInfo";
      this.btnInfo.Size = new System.Drawing.Size(75, 23);
      this.btnInfo.TabIndex = 0;
      this.btnInfo.Text = "Info";
      this.btnInfo.UseVisualStyleBackColor = true;
      this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
      // 
      // tbFrom
      // 
      this.tbFrom.Location = new System.Drawing.Point(21, 12);
      this.tbFrom.Name = "tbFrom";
      this.tbFrom.Size = new System.Drawing.Size(335, 20);
      this.tbFrom.TabIndex = 1;
      this.tbFrom.Text = "Log Form";
      // 
      // tbText
      // 
      this.tbText.Location = new System.Drawing.Point(21, 38);
      this.tbText.Multiline = true;
      this.tbText.Name = "tbText";
      this.tbText.Size = new System.Drawing.Size(335, 94);
      this.tbText.TabIndex = 2;
      this.tbText.Text = "Testing Topic";
      // 
      // btnWarning
      // 
      this.btnWarning.Location = new System.Drawing.Point(102, 155);
      this.btnWarning.Name = "btnWarning";
      this.btnWarning.Size = new System.Drawing.Size(75, 23);
      this.btnWarning.TabIndex = 3;
      this.btnWarning.Text = "Warning";
      this.btnWarning.UseVisualStyleBackColor = true;
      this.btnWarning.Click += new System.EventHandler(this.btnWarning_Click);
      // 
      // btnError
      // 
      this.btnError.Location = new System.Drawing.Point(183, 155);
      this.btnError.Name = "btnError";
      this.btnError.Size = new System.Drawing.Size(75, 23);
      this.btnError.TabIndex = 4;
      this.btnError.Text = "Error";
      this.btnError.UseVisualStyleBackColor = true;
      this.btnError.Click += new System.EventHandler(this.btnError_Click);
      // 
      // btnCatastrophy
      // 
      this.btnCatastrophy.Location = new System.Drawing.Point(264, 155);
      this.btnCatastrophy.Name = "btnCatastrophy";
      this.btnCatastrophy.Size = new System.Drawing.Size(75, 23);
      this.btnCatastrophy.TabIndex = 5;
      this.btnCatastrophy.Text = "Catastrophy";
      this.btnCatastrophy.UseVisualStyleBackColor = true;
      this.btnCatastrophy.Click += new System.EventHandler(this.btnCatastrophy_Click);
      // 
      // btnDebug
      // 
      this.btnDebug.Location = new System.Drawing.Point(65, 211);
      this.btnDebug.Name = "btnDebug";
      this.btnDebug.Size = new System.Drawing.Size(75, 23);
      this.btnDebug.TabIndex = 6;
      this.btnDebug.Text = "Debug";
      this.btnDebug.UseVisualStyleBackColor = true;
      this.btnDebug.Click += new System.EventHandler(this.btnDebug_Click);
      // 
      // btnTrace
      // 
      this.btnTrace.Location = new System.Drawing.Point(146, 211);
      this.btnTrace.Name = "btnTrace";
      this.btnTrace.Size = new System.Drawing.Size(75, 23);
      this.btnTrace.TabIndex = 7;
      this.btnTrace.Text = "Trace";
      this.btnTrace.UseVisualStyleBackColor = true;
      this.btnTrace.Click += new System.EventHandler(this.btnTrace_Click);
      // 
      // btnWebCall
      // 
      this.btnWebCall.Location = new System.Drawing.Point(431, 192);
      this.btnWebCall.Name = "btnWebCall";
      this.btnWebCall.Size = new System.Drawing.Size(75, 23);
      this.btnWebCall.TabIndex = 8;
      this.btnWebCall.Text = "Web Call";
      this.btnWebCall.UseVisualStyleBackColor = true;
      this.btnWebCall.Click += new System.EventHandler(this.btnWebCall_Click);
      // 
      // LogForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(828, 284);
      this.Controls.Add(this.btnWebCall);
      this.Controls.Add(this.btnTrace);
      this.Controls.Add(this.btnDebug);
      this.Controls.Add(this.btnCatastrophy);
      this.Controls.Add(this.btnError);
      this.Controls.Add(this.btnWarning);
      this.Controls.Add(this.tbText);
      this.Controls.Add(this.tbFrom);
      this.Controls.Add(this.btnInfo);
      this.Name = "LogForm";
      this.Text = "LogForm";
      this.ResumeLayout(false);
      this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.TextBox tbFrom;
        private System.Windows.Forms.TextBox tbText;
        private System.Windows.Forms.Button btnWarning;
        private System.Windows.Forms.Button btnError;
        private System.Windows.Forms.Button btnCatastrophy;
        private System.Windows.Forms.Button btnDebug;
        private System.Windows.Forms.Button btnTrace;
    private System.Windows.Forms.Button btnWebCall;
  }
}