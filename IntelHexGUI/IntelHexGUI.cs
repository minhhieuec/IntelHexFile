using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;

using IntelHex;

namespace IntelHexGUI
{
    public partial class IntelHexGUI : Form
    {
        private IntelHexFile hexf = new IntelHexFile();

        public IntelHexGUI()
        {
            InitializeComponent();
            dataGridView.DataSource = hexf.Blocks;
            comboBox.DataSource = Enum.GetValues(typeof(eHash));
        }

        private void loadFile_Click(object sender, EventArgs e)
        {
            Button bt = sender as Button;
            if (bt != null)
            {
                bool append = (bt.Text == "Append");

                try
                {
                    openFileDialog.Filter = "IntelHex Files|*.hex";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Enable to monitor the decode performance
                        //var watch = System.Diagnostics.Stopwatch.StartNew();

                        hexf.Load(openFileDialog.FileName, (eHash)comboBox.SelectedItem, append);

                        //watch.Stop();
                        //Console.WriteLine(watch.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
            }
        }

        private void hashType_SelectedIndexChanged(object sender, EventArgs e)
        {
            hexf.UpdateHash((eHash)comboBox.SelectedItem);
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (dataGridView.SelectedRows.Count != 1)
            {
                e.Cancel = true;
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hexf.Blocks.RemoveAt(dataGridView.SelectedRows[0].Index);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "Binary Files|*.bin|IntelHex Files|*.hex";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.FileName = hexf.Blocks[dataGridView.SelectedRows[0].Index].DisplayAddress;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var extension = Path.GetExtension(saveFileDialog.FileName);
                switch (extension)
                {
                    case ".bin":
                        File.WriteAllBytes(saveFileDialog.FileName, hexf.Blocks[dataGridView.SelectedRows[0].Index].GetBytes());
                        break;
                    case ".hex":
                        IntelHexFile.SaveAsIntelHex(saveFileDialog.FileName, new BinaryBlock[]{ hexf.Blocks[dataGridView.SelectedRows[0].Index]});
                        break;
                    default:
                        MessageBox.Show("Unsupported file extention");
                        return;
                }
            }
        }

        private void copyHashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(hexf.Blocks[dataGridView.SelectedRows[0].Index].DisplayHash);
        }

        private void saveFileHex_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "IntelHex Files|*.hex";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.FileName = "NewHexFile";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var extension = Path.GetExtension(saveFileDialog.FileName);
                switch (extension)
                {
                    case ".hex":
                        IntelHexFile.SaveAsIntelHex(saveFileDialog.FileName, hexf.Blocks.ToArray());
                        break;
                    default:
                        MessageBox.Show("Unsupported file extention");
                        return;
                }
            }
        }
    }
}
