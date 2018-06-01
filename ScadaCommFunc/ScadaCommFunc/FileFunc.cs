using System;
using System.IO;
using System.Xml.Serialization;

namespace ScadaCommFunc
{
    public static class FileFunc
    {
        /// <summary>
        ///  Сериализует объект классов в XML файл
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="filename"></param>
        /// <returns></returns>

        public static bool SaveXml(object obj, string filename)
        {
            bool result = false;
            using (StreamWriter writer = new StreamWriter(filename))
            {
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    XmlSerializer serializer = new XmlSerializer(obj.GetType());
                    serializer.Serialize(writer, obj, ns);
                    result = true;
                    writer.Close();
            }
            return result;
        }

        /// <summary>
        /// Чтение файла XML и десериализация объекта в классы
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filename"></param>
        /// <returns></returns>

        public static object LoadXml(Type type, string filename)
        {
            object result = null;
            using (StreamReader reader = new StreamReader(filename))
            {
                    XmlSerializer serializer = new XmlSerializer(type);
                    result = serializer.Deserialize(reader);
                    reader.Close();
            }
            return result;
        }
    }
}
