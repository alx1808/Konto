using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Globalization;

namespace Konto
{
    public partial class Form1 : Form
    {

        #region Constants
        //private const string DATABASE = @"C:\Users\a.ausweger\Documents\Div\Konto monatlich\Konto2015.xml";
        private const string DATABASE = @"C:\Daten zu Sichern\a.ausweger\Documents\Div\Konto monatlich\Konto.xml";

        #endregion

        #region Member variables
        private List<Record> _AllRecords = new List<Record>();

        #endregion

        #region Constructor

        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region Usercontrol

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(_AllRecords.GetType());
                using (TextWriter writer = new StreamWriter(DATABASE))
                {
                    serializer.Serialize(writer, _AllRecords);
                }

            }
            catch (Exception ex)
            {
                string exMessages = RecGetMessages(ex);
                MessageBox.Show(exMessages);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.Text = "Konto: " + DATABASE;

                LoadData();


                UpdateDataGrid();

            }
            catch (Exception ex)
            {

                string exMessages = RecGetMessages(ex);
                MessageBox.Show(exMessages);
            }
        }

        private void UpdateDataGrid()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = _AllRecords;

            DataGridViewColumn dgvc = dataGridView1.Columns[dataGridView1.Columns.Count - 1];

            dataGridView1.Columns.RemoveAt(dataGridView1.Columns.Count - 1);


            foreach (DataGridViewColumn dgc in dataGridView1.Columns)
            {
                dgc.ReadOnly = true;
                dgc.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                dgc.SortMode = DataGridViewColumnSortMode.Automatic;
            }
            dataGridView1.Columns["Belegdaten"].AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            dataGridView1.Columns["Belegdaten"].Width = 10;
            DataGridViewComboBoxColumn cmb = new DataGridViewComboBoxColumn();
            cmb.HeaderText = "Kategorie";
            cmb.Name = "Kategorie";
            cmb.MaxDropDownItems = 4;
            cmb.DataPropertyName = dgvc.DataPropertyName;
            foreach (var en in Enum.GetValues(typeof(Record.Category)))
            {
                cmb.Items.Add(en);
            }
            cmb.Sorted = true;
            cmb.AutoComplete = true;
            cmb.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            cmb.Width = 100;
            cmb.SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns.Insert(0, cmb);

            WriteStatus();


        }

        private void WriteStatus()
        {
            int anz = _AllRecords.Count;
            decimal einnahmen = Einnahmen();
            decimal ausgaben = Ausgaben();

            lblNrRows.Text = string.Format(CultureInfo.CurrentCulture, "Anzahl der Enträge: {0}, Einnahme = {1}, Ausgaben = {2}", _AllRecords.Count, einnahmen, ausgaben);
        }


        private void bntImport_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.InitialDirectory = @"C:\Daten zu Sichern\a.ausweger\Documents\Div\Konto monatlich";
                openFileDialog1.Title = "CSV-Datei";
                openFileDialog1.Filter = "(*.csv)|*.csv|All files (*.*)|*.*";
                DialogResult res = openFileDialog1.ShowDialog(this);
                if (res == System.Windows.Forms.DialogResult.OK)
                {

                    string fileName = openFileDialog1.FileName; // @"C:\Users\a.ausweger\Documents\Div\2015.csv";

                    List<Record> newRecords = Import(fileName); // System.IO.File.ReadAllLines(fileName);

                    // todo: check if alread in allrecords

                    newRecords = newRecords.Where(x => !_AllRecords.Contains(x)).ToList();
                    _AllRecords.AddRange(newRecords);

                    UpdateDataGrid();

                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Anzahl importierter Records: {0}", newRecords.Count));
                }

            }
            catch (Exception ex)
            {
                string exMessages = RecGetMessages(ex);
                MessageBox.Show(exMessages);
            }



        }
        #endregion

        #region Private


        private void LoadData()
        {
            XmlSerializer deserializer = new XmlSerializer(_AllRecords.GetType());
            using (TextReader reader = new StreamReader(DATABASE))
            {
                object obj = deserializer.Deserialize(reader);
                _AllRecords = (List<Record>)obj;
            }
        }

        private string RecGetMessages(Exception ex)
        {
            if (ex == null) return "";
            return ex.Message + " " + RecGetMessages(ex.InnerException);
        }

        //private const int NR_OF_COLUMNS = 15;

        private List<Record> Import(string fileName)
        {
            var enc = GetEncoding(fileName);
            if (enc == null)
            {
                System.Windows.Forms.MessageBox.Show("Es wurde kein passendes Encoding gefunden oder es sind nicht alle Felder vorhanden!");
            }

            //bool isHeaderLine = true;
            List<Record> records = new List<Record>();
            using (StreamReader sr = new StreamReader(fileName, enc))
            {
                string header = sr.ReadLine();

                int nrOfColumns;
                int nrOfNotFoundFields;
                var flds2indizes = Record.GetFields2Indizes(header, verbose: true, nrOfColums: out nrOfColumns, nrOfNotFoundFields: out nrOfNotFoundFields);
                while (!sr.EndOfStream)
                {

                    // ALg if Lines contain Newline
                    int sepCnt = 0;
                    StringBuilder sb = new StringBuilder();
                    bool inQuotes = false;
                    while (sepCnt < nrOfColumns)
                    {

                        char c = (char)sr.Read();
                        if (c == '\"') inQuotes = !inQuotes;
                        if (sr.EndOfStream) break;
                        if (c != '\n')
                        {
                            sb.Append(c);
                        }
                        if (c == ';' && !inQuotes)
                        {
                            sepCnt++;
                        }
                    }

                    // am schluss muss eine newline sein
                    while (!sr.EndOfStream && ((char)sr.Peek() != '\n'))
                    {
                        sr.Read();
                    }

                    string s = sb.ToString();

                    // ALG in Lines don't contain newline
                    //string s = sr.ReadLine();

                    if (!string.IsNullOrEmpty(s))
                    {
                        Record rec = null;
                        //if (isHeaderLine) isHeaderLine = false;
                        //else
                        //{
                        rec = new Record(s, flds2indizes);
                        records.Add(rec);
                        //}
                    }
                }
            }
            return records;
        }

        private Encoding GetEncoding(string fileName)
        {

            EncodingInfo[] codePages = Encoding.GetEncodings();
            List<Encoding> encs = new List<Encoding>();
            foreach (EncodingInfo codePage in codePages)
            {
                encs.Add(codePage.GetEncoding());
                //Console.WriteLine("Code page ID: {0}, IANA name: {1}, human-friendly display name: {2}", codePage.CodePage, codePage.Name, codePage.DisplayName);
            }
            //List<Encoding> encs = new List<Encoding>() { Encoding.UTF8, Encoding.ASCII };
            foreach (var enc in encs)
            {
                string header = string.Empty;
                using (StreamReader sr = new StreamReader(fileName, enc))
                {
                    header = sr.ReadLine();

                }

                int nrOfColumns;
                int nrOfNotFoundFields;
                var flds2indizes = Record.GetFields2Indizes(header, verbose: false, nrOfColums: out nrOfColumns, nrOfNotFoundFields: out nrOfNotFoundFields);
                if (nrOfNotFoundFields == 0) return enc;
            }

            return null;
        }

        #endregion

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //if (e.ColumnIndex == 16) return;
            //else 
            throw (e.Exception);
        }

        private void dataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {

        }

        private void dataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 16)
            {
                foreach (Record.Category en in Enum.GetValues(typeof(Record.Category)))
                {
                    if (string.Compare(en.ToString(), e.FormattedValue.ToString()) == 0)
                    {
                        _AllRecords[e.RowIndex].Kategorie = en;
                        break;
                    }
                }


            }
        }

        private decimal Ausgaben()
        {
            return _AllRecords.Where(x => x.Betrag <= 0).Sum(x => x.Betrag);
        }

        private decimal Einnahmen()
        {
            return _AllRecords.Where(x => x.Betrag > 0).Sum(x => x.Betrag);
        }

        private void btnPerKategorie_Click(object sender, EventArgs e)
        {
            var results = from rec in _AllRecords
                          group rec.Betrag by rec.Kategorie into g
                          select new { Kategorie = g.Key, Sum = g.Sum() };

            StringBuilder sb = new StringBuilder();
            foreach (var res in results)
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "{0}\t{1}", res.Kategorie, res.Sum));
            }

            Clipboard.SetText(sb.ToString());

        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            switch (dataGridView1.Columns[e.ColumnIndex].Name)
            {
                case "Buchungsdatum":
                    _AllRecords = _AllRecords.OrderBy(x => x.Buchungsdatum).ToList();
                    dataGridView1.DataSource = null;
                    UpdateDataGrid();
                    break;

                case "Valutadatum":
                    _AllRecords = _AllRecords.OrderBy(x => x.Valutadatum).ToList();
                    dataGridView1.DataSource = null;
                    UpdateDataGrid();
                    break;

                case "Betrag":
                    _AllRecords = _AllRecords.OrderBy(x => x.Betrag).ToList();
                    dataGridView1.DataSource = null;
                    UpdateDataGrid();
                    break;

                case "Kategorie":
                    _AllRecords = _AllRecords.OrderBy(x => x.Betrag).ToList();
                    dataGridView1.DataSource = null;
                    UpdateDataGrid();
                    break;

                case "Buchungstext":
                    _AllRecords = _AllRecords.OrderBy(x => x.Buchungstext).ToList();
                    dataGridView1.DataSource = null;
                    UpdateDataGrid();
                    break;


                default:
                    break;
            }
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = string.Empty;
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CSV-Files|*.csv";
                    dlg.Title = "CSV-Export";
                    var res = dlg.ShowDialog(this);
                    switch (res)
                    {
                        case DialogResult.OK:
                            fileName = dlg.FileName;
                            break;
                        default:
                            return;
                    }
                }

                var lines = _AllRecords.Select(x => x.AsCsv());
                File.WriteAllLines(fileName, lines, Encoding.UTF8);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }



    }
}
