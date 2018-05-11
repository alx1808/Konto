using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Konto
{
    public class Record : IEquatable<Record>
    {
        public enum Category
        {
            Unbekannt,
            Auto,
            Finanzamt,
            GKK,
            Haushalt,
            Honorar,
            HaushaltSonder,
            Kinder,
            Buero,
            ORF,
            Kredit,
            SVA,
            Schule,
            WienerStaedtische,
            Haus,
            Bank,
            Ärzte,
            Eigenübertrag,
            Patricia
        }

        public const string SEPARATOR = ";";
        private static string[] _Sep_Group = { SEPARATOR };
        private const int NR_OF_ENTRIES_PER_LINE = 15;

        public enum Field
        {
            Buchungsdatum,
            Valutadatum,
            Buchungstext,
            InterneNotiz,
            Waehrung,
            Betrag,
            Belegdaten,
            Beleg,
            Auftraggebername,
            Auftraggeberkonto,
            AuftraggeberBLZ,
            Empfaengername,
            Empfaengerkonto,
            EmpfaengerBLZ,
            Zahlungsgrund

        }


        public Record()
        {
        }

        private string GetPart(string[] parts, Dictionary<Field, int> cats2indizes, Field field)
        {
            int index = cats2indizes[field];
            if (index == -1) return string.Empty;
            return parts[index];
        }

        public static Dictionary<Field, int> GetFields2Indizes(string header, bool verbose, out int nrOfColums, out int nrOfNotFoundFields)
        {
            Dictionary<Field, int> dict = new Dictionary<Field, int>();
            string h = header.ToUpperInvariant().Trim();
            if (h.EndsWith(";"))
            {
                h = h.Remove(h.Length - 1);
            }
            List<string> parts = h.Split(_Sep_Group, StringSplitOptions.None).Select(x => x.Trim()).ToList();
            nrOfColums = parts.Count;
            dict[Field.Buchungsdatum] = parts.IndexOf("BUCHUNGSDATUM");
            dict[Field.Valutadatum] = parts.IndexOf("VALUTADATUM");
            dict[Field.Buchungstext] = parts.IndexOf("BUCHUNGSTEXT");
            dict[Field.InterneNotiz] = parts.IndexOf("INTERNE NOTIZ");
            dict[Field.Waehrung] = parts.IndexOf("WÄHRUNG");
            dict[Field.Betrag] = parts.IndexOf("BETRAG");
            dict[Field.Belegdaten] = parts.IndexOf("BELEGDATEN");
            int belegIndex = parts.IndexOf("BELEG");
            if (belegIndex == -1)
            {
                belegIndex = parts.IndexOf("BELEGNUMMER");
            }
            dict[Field.Beleg] = belegIndex;
            dict[Field.Auftraggebername] = parts.IndexOf("AUFTRAGGEBERNAME");
            dict[Field.Auftraggeberkonto] = parts.IndexOf("AUFTRAGGEBERKONTO");
            dict[Field.AuftraggeberBLZ] = parts.IndexOf("AUFTRAGGEBER BLZ");
            dict[Field.Empfaengername] = parts.IndexOf("EMPFÄNGERNAME");
            dict[Field.Empfaengerkonto] = parts.IndexOf("EMPFÄNGERKONTO");
            dict[Field.EmpfaengerBLZ] = parts.IndexOf("EMPFÄNGER BLZ");
            dict[Field.Zahlungsgrund] = parts.IndexOf("ZAHLUNGSGRUND");

            // prepare message
            StringBuilder sb = new StringBuilder();
            List<int> partsInd = new List<int>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    partsInd.Add(i);
                }
            }
            List<string> notFoundFields = new List<string>();
            List<string> notFoundParts = new List<string>();
            foreach (var kvp in dict)
            {
                if (kvp.Value == -1)
                {
                    notFoundFields.Add(kvp.Key.ToString());
                }
                else
                {
                    partsInd.Remove(kvp.Value);
                }
            }
            if (notFoundFields.Count > 0)
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Folgende Felder wurden nicht gefunden: {0}", string.Join(", ", notFoundFields.ToArray())));
                //System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Folgende Felder wurden nicht gefunden: {0}", string.Join(", ", notFoundFields.ToArray())));
            }
            if (partsInd.Count > 0)
            {
                List<string> missingParts = new List<string>();
                foreach (var ind in partsInd)
                {
                    missingParts.Add(parts[ind]);
                }
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Folgende CSV wurden nicht gefunden: {0}", string.Join(", ", missingParts.ToArray())));
            }
            string msg = sb.ToString();
            if (!string.IsNullOrEmpty(msg))
            {
                if (verbose) System.Windows.Forms.MessageBox.Show(msg);
            }
            else
            {
                if (verbose) System.Windows.Forms.MessageBox.Show("Alle Felder wurden gefunden.");
            }

            nrOfNotFoundFields = notFoundFields.Count;
            return dict;
        }

        public string[] SplitLine(string line)
        {
            List<string> parts = new List<string>();
            var chars = line.ToArray();
            bool inQuotes = false;
            string curPart = string.Empty;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (inQuotes)
                {
                    if (c == '\"')
                    {
                        inQuotes = !inQuotes;
                    }
                    curPart += c;
                }
                else
                {
                    if (c == '\"')
                    {
                        inQuotes = !inQuotes;
                        curPart += c;
                    }
                    else
                    {
                        if (c == ';')
                        {
                            parts.Add(curPart);
                            curPart = string.Empty;
                        }
                        else
                        {
                            curPart += c;
                        }
                    }
                }
            }
            parts.Add(curPart);
            return parts.ToArray();
        }

        public Record(string line, Dictionary<Field, int> fields2indizes)
        {
            try
            {
                Kategorie = Category.Unbekannt;

                string[] parts = SplitLine(line); // line.Split(_Sep_Group, StringSplitOptions.None);


                string dateString = GetPart(parts, fields2indizes, Field.Buchungsdatum);
                if (!string.IsNullOrEmpty(dateString))
                {
                    Buchungsdatum = DateTime.Parse(dateString, CultureInfo.CurrentCulture);
                }
                dateString = GetPart(parts, fields2indizes, Field.Valutadatum);
                if (!string.IsNullOrEmpty(dateString))
                {
                    Valutadatum = DateTime.Parse(dateString, CultureInfo.CurrentCulture);
                }

                Buchungstext = GetPart(parts, fields2indizes, Field.Buchungstext);
                InterneNotiz = GetPart(parts, fields2indizes, Field.InterneNotiz);
                Waehrung = GetPart(parts, fields2indizes, Field.Waehrung);

                string betrString = GetPart(parts, fields2indizes, Field.Betrag);
                //var style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;
                var style = NumberStyles.Currency;


                NumberFormatInfo MyNFI = new NumberFormatInfo(); // CultureInfo.CurrentCulture.NumberFormat;
                MyNFI.NegativeSign = "-";
                MyNFI.CurrencyDecimalSeparator = ",";
                MyNFI.CurrencyGroupSeparator = ".";
                MyNFI.NumberDecimalSeparator = ",";
                MyNFI.NumberGroupSeparator = ".";

                Betrag = decimal.Parse(betrString,style, MyNFI);

                Belegdaten = GetPart(parts, fields2indizes, Field.Belegdaten);
                Beleg = GetPart(parts, fields2indizes, Field.Beleg);
                Auftraggebername = GetPart(parts, fields2indizes, Field.Auftraggebername);
                Auftraggeberkonto = GetPart(parts, fields2indizes, Field.Auftraggeberkonto);
                AuftraggeberBLZ = GetPart(parts, fields2indizes, Field.AuftraggeberBLZ);
                Empfaengername = GetPart(parts, fields2indizes, Field.Empfaengername);
                Empfaengerkonto = GetPart(parts, fields2indizes, Field.Empfaengerkonto);
                EmpfaengerBLZ = GetPart(parts, fields2indizes, Field.EmpfaengerBLZ);
                Zahlungsgrund = GetPart(parts, fields2indizes, Field.Zahlungsgrund);

            }
            catch (Exception ex)
            {
                throw new WrongRecordFormatException(line, ex);
            }


        }
        [Obsolete()]
        public Record(string line)
        {
            try
            {
                Kategorie = Category.Unbekannt;

                string[] parts = line.Split(_Sep_Group, StringSplitOptions.None);
                if (parts.Length != NR_OF_ENTRIES_PER_LINE + 1) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Zeile hat weniger als {0} Einträge!", NR_OF_ENTRIES_PER_LINE));

                string dateString = parts[0];
                if (!string.IsNullOrEmpty(dateString))
                {
                    Buchungsdatum = DateTime.Parse(dateString, CultureInfo.CurrentCulture);
                }
                dateString = parts[1];
                if (!string.IsNullOrEmpty(dateString))
                {
                    Valutadatum = DateTime.Parse(dateString, CultureInfo.CurrentCulture);
                }

                Buchungstext = parts[2];
                InterneNotiz = parts[3];
                Waehrung = parts[4];

                Betrag = decimal.Parse(parts[5], CultureInfo.CurrentCulture);

                Belegdaten = parts[6];
                Beleg = parts[7];
                Auftraggebername = parts[8];
                Auftraggeberkonto = parts[9];
                AuftraggeberBLZ = parts[10];
                Empfaengername = parts[11];
                Empfaengerkonto = parts[12];
                EmpfaengerBLZ = parts[13];
                Zahlungsgrund = parts[14];

            }
            catch (Exception ex)
            {
                throw new WrongRecordFormatException(line, ex);
            }


        }

        public string AsCsv()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(WrapText(Kategorie.ToString()));
            sb.Append(";");

            sb.Append(Buchungsdatum.ToString(CultureInfo.CurrentCulture));
            sb.Append(";");

            sb.Append(Valutadatum.ToString(CultureInfo.CurrentCulture));
            sb.Append(";");

            sb.Append(WrapText(Buchungstext));
            sb.Append(";");

            sb.Append(WrapText(InterneNotiz));
            sb.Append(";");

            sb.Append(Betrag.ToString(CultureInfo.CurrentCulture));
            sb.Append(";");

            sb.Append(WrapText(Belegdaten));
            sb.Append(";");

            string normBelegDaten = Belegdaten.Trim();
            normBelegDaten = normBelegDaten.Trim().Trim(new char[] { '"' }).Trim();
            
            
            string beleg = string.IsNullOrEmpty(normBelegDaten) ? "" : "B" + Beleg;
            sb.Append(WrapText(beleg));
            sb.Append(";");

            sb.Append(WrapText(Auftraggebername));
            sb.Append(";");

            sb.Append(WrapText(Auftraggeberkonto));
            sb.Append(";");

            sb.Append(WrapText(AuftraggeberBLZ));
            sb.Append(";");

            sb.Append(WrapText(Empfaengername));
            sb.Append(";");

            sb.Append(WrapText(Empfaengerkonto));
            sb.Append(";");

            sb.Append(WrapText(EmpfaengerBLZ));
            sb.Append(";");

            sb.Append(WrapText(Zahlungsgrund));

            return sb.ToString();
        }

        private string WrapText(string txt)
        {
            StringBuilder sb = new StringBuilder();
            if (!txt.StartsWith("\""))
            {
                sb.Append("\"");
            }
            sb.Append(txt);
            if (!txt.EndsWith("\""))
            {
                sb.Append("\"");
            }

            return sb.ToString();
        }

        public DateTime Buchungsdatum { get; set; }
        public DateTime Valutadatum { get; set; }
        public string Buchungstext { get; set; }
        public string InterneNotiz { get; set; }
        public string Waehrung { get; set; }
        public decimal Betrag { get; set; }
        public string Belegdaten { get; set; }
        public string Beleg { get; set; }
        public string Auftraggebername { get; set; }
        public string Auftraggeberkonto { get; set; }
        public string AuftraggeberBLZ { get; set; }
        public string Empfaengername { get; set; }
        public string Empfaengerkonto { get; set; }
        public string EmpfaengerBLZ { get; set; }
        public string Zahlungsgrund { get; set; }

        public Category Kategorie { get; set; }

        #region IEquatable
        public bool Equals(Record other)
        {
            if ((this.Buchungsdatum == other.Buchungsdatum) &&
                (this.Valutadatum == other.Valutadatum) &&
                (this.Buchungstext == other.Buchungstext) &&
                (this.InterneNotiz == other.InterneNotiz) &&
                (DecEqual(this.Betrag, other.Betrag)) &&
                (this.Belegdaten == other.Belegdaten) &&
                (this.Beleg == other.Beleg) &&
                (this.Auftraggebername == other.Auftraggebername) &&
                (this.Auftraggeberkonto == other.Auftraggeberkonto) &&
                (this.AuftraggeberBLZ == other.AuftraggeberBLZ) &&
                (this.Empfaengername == other.Empfaengername) &&
                (this.Empfaengerkonto == other.Empfaengerkonto) &&
                (this.EmpfaengerBLZ == other.EmpfaengerBLZ) &&
                (this.Zahlungsgrund == other.Zahlungsgrund)
                )
                return true;
            else return false;
        }

        private const decimal DEC_EPS = 0.001M;

        private bool DecEqual(decimal d1, decimal d2)
        {
            return Math.Abs(d1 - d2) < DEC_EPS;
        }

        #endregion
    }
}
