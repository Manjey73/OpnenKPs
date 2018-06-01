using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ScadaCommFunc
{
    public class FileFunc
    {
        public static bool SaveXml(object obj, string filename)
        {
            bool result = false;
            using (StreamWriter writer = new StreamWriter(filename))
            {
                try
                {
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    XmlSerializer serializer = new XmlSerializer(obj.GetType());
                    serializer.Serialize(writer, obj, ns);
                    result = true;
                }
                catch (Exception ex)
                {
                    // Логирование
                }
                finally
                {
                    writer.Close();
                }
            }
            return result;
        }

        public static object LoadXml(Type type, string filename)
        {
            object result = null;
            using (StreamReader reader = new StreamReader(filename))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    result = serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    // Логирование
                }
                finally
                {
                    reader.Close();
                }
            }
            return result;
        }

    }
}
