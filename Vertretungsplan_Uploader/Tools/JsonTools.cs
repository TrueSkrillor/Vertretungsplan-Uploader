using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace Vertretungsplan_Uploader.Tools
{
    public static class JsonTools
    {
        public static void GenerateJson(string pPath, string pFilename)
        {
            StringBuilder builder = new StringBuilder();
            JsonWriter writer = new JsonTextWriter(new StringWriter(builder));

            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();

            string currentLine = "";
            StreamReader reader = new StreamReader(pPath + "/" + pFilename + ".html", Encoding.Default);

            while((currentLine = reader.ReadLine()) != null)
            {
                if (currentLine.ToLower().Contains("width=\"34%\""))
                {
                    writer.WritePropertyName("Erstellungsdatum");
                    string rawDate = currentLine.Split('>')[1].Split('<')[0];
                    writer.WriteValue(rawDate.Substring(rawDate.IndexOf(' ') + 1));
                }
                else if (currentLine.ToLower().Contains("vertretungsplan für") || currentLine.ToLower().Contains("vertretungsplan f&uuml;r")) {
                    writer.WritePropertyName("Datum");
                    writer.WriteValue(currentLine.Split(',')[1].Substring(1).Split('<')[0]);
                }
                else if (currentLine.ToLower().Contains("mitteilungen"))
                {
                    writer.WritePropertyName("Mitteilungen");
                    writer.WriteStartArray();
                    while (!(currentLine = reader.ReadLine()).ToLower().Contains("</table>"))
                    {
                        if(currentLine.ToLower().Contains("<td>"))
                        {
                            string[] solo = currentLine.Split(new string[] { "<br><br>", "<BR><BR>" }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < solo.Length; i++) {
                                solo[i] = solo[i].Replace("<br>", " ").Replace("<BR>", " ");
                                if (i == 0)
                                    solo[i] = solo[i].Split('>')[1];
                                writer.WriteValue(solo[i]);
                            }
                        }
                    }
                    writer.WriteEndArray();
                }
                else if (currentLine.ToLower().Contains("bordercolor=\"black\" cellpadding=\"4\""))
                {
                    writer.WritePropertyName("Vertretungen");
                    writer.WriteStartArray();
                    while ((currentLine = reader.ReadLine()) != null && !currentLine.ToLower().Contains("<tbody>")) { }

                    string previous = "";
                    while (!(currentLine = reader.ReadLine()).ToLower().Contains("</tbody>"))
                    {
                        if(currentLine.ToLower().Contains("<tr>"))
                        {
                            currentLine = reader.ReadLine();
                            if (!currentLine.ToLower().Contains("colspan=\"7\""))
                            {
                                writer.WriteStartObject();
                                writer.WritePropertyName("Klasse");
                                if (currentLine.ToLower().Contains("<b>"))
                                {
                                    previous = currentLine.Split('>')[2].Split('<')[0].Replace(" ", "");
                                    writer.WriteValue(previous);
                                }
                                else
                                    writer.WriteValue(previous);

                                writer.WritePropertyName("Stunde");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);

                                writer.WritePropertyName("Vertretung");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);

                                writer.WritePropertyName("Fach");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);

                                writer.WritePropertyName("statt");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);

                                writer.WritePropertyName("Raum");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);

                                writer.WritePropertyName("Sonstiges");
                                writer.WriteValue(reader.ReadLine().Split('>')[1].Split('<')[0]);
                             
                                writer.WriteEndObject();
                            }
                        }
                    }
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
            reader.Close();

            using (StreamWriter output = new StreamWriter(pPath + "/" + pFilename + ".json"))
            {
                output.Write(ReplaceHTMLTags(builder.ToString()));
            }
        }

        private static string ReplaceHTMLTags(string pString) { return pString.Replace("&auml;", "ä").Replace("&Auml;", "Ä").Replace("&Ouml;", "Ö").Replace("&ouml;", "ö").Replace("&Uuml;", "Ü").Replace("&uuml;", "ü").Replace("&nbsp;", ""); }
    }
}
