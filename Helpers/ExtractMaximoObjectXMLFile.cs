using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace IntegrationService.Helpers
{
    class ExtractMaximoObjectXMLFile
    {
        public static Models.MaximoAssetData parseMaximoXML(string file, NLog.ILogger log)
        {
            log.Info("Reading Maximo XML import file " + file);

            int nErrorPos = 1;

            Models.MaximoAssetData mad = new Models.MaximoAssetData();
            XmlDocument doc = new XmlDocument();
            XmlNodeList currNode;
            int intFileType = 0;

            //open the file, do a search/replace of encoding="UTF-8" with encoding="ISO8859-1"
            try
            {
                log.Debug("Maximo XML Conversion utf to iso");
                String xmlBuffer;
                nErrorPos = 14;
                using (StreamReader sread = new StreamReader(file, Encoding.Default))
                {
                    xmlBuffer = sread.ReadToEnd();

                }
                xmlBuffer = xmlBuffer.Replace("encoding=\"UTF-8", "encoding=\"ISO8859-1");
                FileStream nfs;
                nfs = new FileStream(file, FileMode.OpenOrCreate);
                using (StreamWriter swrite = new StreamWriter(nfs, Encoding.Default))
                {
                    swrite.Write(xmlBuffer);
                    //swrite.Close();
                }
                nfs.Dispose();
            }
            catch(Exception e)
            {
                log.Error("Maximo XML import file access error, please check the source file location/rights. Current file moved to Rejected");
                return null;
            }

            try
            {
                nErrorPos = 2;
                doc.Load(file);
            }
            catch
            {
                log.Error("Maximo XML import file conversion error, please check the source file. Current file moved to Rejected");
                return null;
            }

            try
            {
                //The statement below is not valid:
                //Determine if we have a "new asset" file or an "asset up" file (the "asset up" file doesn't have a ASSETMETER node - and if trying to select one it will fail... - catch that
                //Instead, use filetype to trigger the reading of tags belonging to meters
                //The filetype is always "2" in the csv file.
                nErrorPos = 3;

                currNode = doc.GetElementsByTagName("ASSETMETER");

                if (currNode.Count > 0 && currNode.Item(0).InnerText.Length > 0)
                    intFileType = 1;
            }
            catch
            {
                intFileType = 2;
            }

            try
            {
                log.Debug("Compiling header data");
                //FILETYPE(1);ASSETNUM;SITEID;HIERARCHYPATH;ISRUNNING;ORGID;PARENT(alltid tom);STATUS;METERNAME;METERROW;UNITSTOGO;FREQUENCY
                //FILETYPE(2);ASSETNUM;SITEID;HIERARCHYPATH(tom);ISRUNNING;ORGID;PARENT(tom);STATUS(tom);METERNAME(tom);METERROW(tom);UNITSTOGO(tom);FREQUENCY(tom)

                nErrorPos = 4;

                mad.filename = Path.GetFileName(file);

                nErrorPos = 5;

                //Always type 2 (assume update) - import sql sets To 1 (add) if not found
                mad.FileType = 2; //intFileType;

                nErrorPos = 6;

                currNode = doc.GetElementsByTagName("ASSETNUM");
                mad.AssetNum = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 7;

                currNode = doc.GetElementsByTagName("SITEID");
                mad.SiteId = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 8;

                currNode = doc.GetElementsByTagName("ISRUNNING");
                mad.IsRunning = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 9;

                currNode = doc.GetElementsByTagName("ORGID");
                mad.OrgId = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 10;

                currNode = doc.GetElementsByTagName("C_ISMES");
                mad.C_IsMes = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 11;

                currNode = doc.GetElementsByTagName("C_MESTYPE");
                mad.C_MesType = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 12;

                currNode = doc.GetElementsByTagName("PARENT");
                mad.Parent = currNode.Item(0).InnerText.Replace(";", "&#59");

                nErrorPos = 13;

                currNode = doc.GetElementsByTagName("STATUS");
                mad.Status = currNode.Item(0).InnerText.Replace(";", "&#59");

            }
            catch
            {
                log.Error("ASSET data missing or corrupt, please check the source file. Current file moved to Rejected");
                log.Error("nErrorPos: " + nErrorPos.ToString());
                return null;
            }


            try
            {
                if (intFileType == 1)
                {
                    log.Debug("Compiling ASSETMETER data");
                    currNode = doc.GetElementsByTagName("UNITSTOGO");
                    int maxindex = currNode.Count;
                    if (maxindex > 0)
                    {
                        //debug:log.writeToLog("maxindex =" + maxindex.ToString(), config.getLogModeExtended());
                        for (int i = 0; i < maxindex; i++)
                        {
                            Models.MaximoMeterData md = new Models.MaximoMeterData();

                            //debug:log.writeToLog(currNode.Item(i).InnerText.Replace(";", "&#59"), config.getLogModeExtended());

                            md.MeterRow = i + 1;

                            currNode = doc.GetElementsByTagName("METERNAME");
                            md.MeterName = currNode.Item(0).InnerText.Replace(";", "&#59");

                            currNode = doc.GetElementsByTagName("UNITSTOGO");
                            md.UnitsToGo = currNode.Item(i).InnerText.Replace(";", "&#59");

                            currNode = doc.GetElementsByTagName("FREQUENCY");
                            md.Frequency = currNode.Item(i).InnerText.Replace(";", "&#59");

                            mad.MeterData.Add(md);
                        }
                    }
                    else
                    {
                        Models.MaximoMeterData md = new Models.MaximoMeterData();

                        //debug:log.writeToLog(currNode.Item(i).InnerText.Replace(";", "&#59"), config.getLogModeExtended());

                        md.MeterRow = 1;
                        md.MeterName = "";
                        md.UnitsToGo = "";
                        md.Frequency = "";
                        mad.MeterData.Add(md);
                    }

                }
            }
            catch
            {
                log.Error("ASSETMETER data missing or corrupt, please check the source file. Current file moved to Rejected");
                return null;
            }

            return mad;


        }
    }
}
