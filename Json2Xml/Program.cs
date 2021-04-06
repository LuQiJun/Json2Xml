using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Json2Xml
{
    class Program
    {
        public static Stack<string> signStack = new Stack<string>();//存放符号栈
        public static Stack<string> jsonArrStack = new Stack<string>();//存放数组对象的名字的栈

        public static List<byte> quoteContentBytes = new List<byte>();//存放引号中的数据

        static bool isKey = true;//用来判断是否是key
        static bool isTextNode = false;//用来判断是否是值节点

        static bool isAlreadyWriteFirstStart = false; //用来判断是否是值节点数组
        static int valueArrDeepth = 0;

        static string tempAttr = "";//存放临时属性
        static string tempElement = "";//存放临时元素名
        static bool isTempElementBefore = false;
        static bool isCommaBefore = false;//用来判断某项操作前的一个符号是否是一个逗号

        static void Main(string[] args)
        {
            Console.WriteLine("请输入Json文件路径：");
            string path = Console.ReadLine();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //解析Json
            JsonDataToXml(path);

            sw.Stop();
            Console.WriteLine("用时：" + sw.Elapsed.ToString());

            Console.WriteLine("解析成功！");
            Console.ReadKey();
        }

        public static void JsonDataToXml(string path)
        {
            using (FileStream fsRead = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                //为XmlReader对象设置settings
                XmlReaderSettings settingsReader = new XmlReaderSettings();
                settingsReader.IgnoreComments = true;
                settingsReader.IgnoreWhitespace = true;

                //为XmlWriter对象设置settings
                XmlWriterSettings settingsWriter = new XmlWriterSettings();
                settingsWriter.Indent = true;//要求缩进
                settingsWriter.Encoding = new UTF8Encoding(true);
                settingsWriter.NewLineChars = Environment.NewLine; //设置换行符

                string filePath = path.Substring(0, path.LastIndexOf("."));
                string writerPath = filePath + ".xml";

                using (XmlWriter xmlWriter = XmlWriter.Create(writerPath, settingsWriter))
                {
                    xmlWriter.WriteStartDocument(false);

                    int bt = fsRead.ReadByte();
                    while (bt != -1)
                    {
                        //为冒号“:”
                        if (bt == 58)
                            isKey = false;

                        if (bt == 34)//为引号
                        {
                            int b = fsRead.ReadByte();

                            //读取引号中的内容
                            while (b != 34)
                            {
                                quoteContentBytes.Add(Convert.ToByte(b));
                                b = fsRead.ReadByte();
                            }
                            string result = System.Text.Encoding.UTF8.GetString(quoteContentBytes.ToArray());
                            if (signStack.Peek() == "[")
                            {
                                if (!isAlreadyWriteFirstStart)
                                    xmlWriter.WriteStartElement(jsonArrStack.Peek());
                                isAlreadyWriteFirstStart = false;
                                xmlWriter.WriteString(result);
                                xmlWriter.WriteEndElement();
                            }
                            else
                            {
                                if (isKey)//为key
                                {
                                    //是属性Attribute
                                    if (result[0].ToString() == "-")
                                    {
                                        string attr = Regex.Replace(result, "-", "");
                                        tempAttr = attr;

                                        isTempElementBefore = false;
                                    }
                                    else if (result != "#text")//是子节点
                                    {
                                        tempElement = result;
                                        xmlWriter.WriteStartElement(tempElement);

                                        isTempElementBefore = true;
                                    }
                                    else//是包含Value的节点
                                        isTextNode = true;
                                }
                                else//为值
                                {
                                    if (isTextNode || isTempElementBefore)
                                    {
                                        xmlWriter.WriteString(result);
                                        isTextNode = false;
                                        if (isTempElementBefore)
                                            xmlWriter.WriteEndElement();
                                    }
                                    else
                                        xmlWriter.WriteAttributeString(tempAttr, result);
                                }
                            }

                            quoteContentBytes.Clear();//清空
                            isCommaBefore = false;
                        }
                        else if (bt == 123)//为“{”
                        {
                            //若是逗号，则表示是一个[]对象中的除了第一个的其他对象。
                            if (isCommaBefore)
                                xmlWriter.WriteStartElement(jsonArrStack.Peek());

                            signStack.Push("{");//符号进栈
                            isKey = true;

                            isAlreadyWriteFirstStart = false;
                            isTempElementBefore = false;
                        }
                        else if (bt == 91)
                        {
                            signStack.Push("[");//符号进栈
                            jsonArrStack.Push(tempElement);//此处tempElement存放便是“[”之前的一个引号中的数据

                            isAlreadyWriteFirstStart = true;

                            isTempElementBefore = false;

                        }
                        else if (bt == 125)//为“}”时,jsonArrStack退栈,并写入WriteEndElement
                        {
                            string t = signStack.Pop();//符号出栈
                                                       //自检,用于检测是否有括号不匹配现象
                            if ((bt == 125 && t != "{") || (bt == 93 && t != "["))
                                Console.WriteLine(tempElement);

                            //如果是最后一个“{”，对应于第一个进栈的，也不写关闭元素；
                            if (signStack.Count > 1)
                            {
                                xmlWriter.WriteEndElement();
                                isAlreadyWriteFirstStart = signStack.Peek() != "[";
                            }

                        }
                        else if (bt == 93) //为“]”时,jsonArrStack退栈
                        {
                            string t = signStack.Pop();//符号出栈
                                                       //自检,用于检测是否有括号不匹配现象
                            if ((bt == 125 && t != "{") || (bt == 93 && t != "["))
                                Console.WriteLine(tempElement);

                            //若为“]”,则arrStack出栈
                            jsonArrStack.Pop();

                            isAlreadyWriteFirstStart = signStack.Peek() != "[";
                        }

                        //逗号
                        if (bt == 44)
                        {
                            isKey = true;
                            isCommaBefore = true;//记录下一次读取数据之前的符号为逗号
                        }
                        bt = fsRead.ReadByte();
                    }
                }
            }
        }
    }
}
