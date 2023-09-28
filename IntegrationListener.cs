using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using SqlDependancyTest.Models;
using SqlDependancyTest.Models.DTO;
using AutoMapper;
using System.Collections.Generic;
using IntegrationService.Domain;
using System.Xml.Linq;
using NLog;
using Microsoft.Extensions.Configuration;
using System.Timers;
using System.Threading;

namespace IntegrationService
{
    internal class IntegrationListener : IDisposable
    {
        private System.Timers.Timer _t;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private string _connectionString;
        private string _queueName;
        private NLog.ILogger _log;
        private string _replaceDelimiter = "@";
        private string _databaseName = "";
        private SqlDependencyEx _sqlDependencyOut;
        private SqlDependencyEx _sqlDependencyIn;
        private Mapper mapper;

        private System.Timers.Timer eamInTimer = new System.Timers.Timer();
        private List<Models.MaximoAssetData> eamInFromCurrentQuantum = new List<Models.MaximoAssetData>();
        private readonly object eamInLock = new object();
        bool _dataFeedRunning, _dataFeedReRun;
        private string templateDirectory;
        private string remoteDirectory;
        private string localDirectory;
        FileSystemWatcher _fswDataFeed = new FileSystemWatcher();




        public IntegrationListener(int order = 1)
        {
            _t = new System.Timers.Timer();
            _t.AutoReset = false;
            _t.Elapsed += _t_Elapsed;

            _log = LogManager.GetCurrentClassLogger();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<IntegrationMessage, IntegrationMessageDTO>();
                cfg.CreateMap<IntegrationMessageDTO, IntegrationMessage>();
                cfg.CreateMap<IntegrationMessageParameter, IntegrationMessageParameterDTO>();
                cfg.CreateMap<IntegrationMessageParameterDTO, IntegrationMessageParameter>();
                cfg.CreateMap<MessageTrigger, MessageTriggerDTO>();
                cfg.CreateMap<MessageTriggerDTO, MessageTrigger>();
            });

            mapper = new Mapper(config);

            try
            {
                bool isTest = Program.Config.GetValue<bool>("appSettings:isTest");
                string system = isTest ? "test" : "prod";
               
                #region EAM Import configuration
                bool isImportEAMEnabled = Program.Config.GetValue<bool>("appSettings:isImportEAMEnabled");

                string incomingEAMFolderPath = Program.Config.GetValue<string>("appSettings:incomingEAMFolderPath");
                localDirectory = Program.Config.GetValue<string>("appSettings:baseEAMFolderPath");

                #endregion EAM Import configuration

                int timerInterval = Program.Config.GetValue<int>("appSettings:timerIntervalSeconds");
                this._connectionString = Program.Config.GetValue<string>($"connectionStrings:{system}");

                var builder = new SqlConnectionStringBuilder(_connectionString);
                this._databaseName = builder.InitialCatalog;

                if (isImportEAMEnabled)
                {
                    if (!string.IsNullOrEmpty(incomingEAMFolderPath)) //This condition to handle if config file doesn't have the DataInput folder.
                    _fswDataFeed.Path = incomingEAMFolderPath;
                    _fswDataFeed.Filter = "*.*";
                    _fswDataFeed.EnableRaisingEvents = true;
                    _fswDataFeed.Created += new FileSystemEventHandler(fswDataFeedCreated);
                    FileSystemEventArgs fsdatafeedEvents = new FileSystemEventArgs(WatcherChangeTypes.Created, _fswDataFeed.Path, _fswDataFeed.Path);
                    fswDataFeedCreated(_fswDataFeed, fsdatafeedEvents);
                    _log.Debug("File watcher created on :" + incomingEAMFolderPath);
                }

                _t.Interval = timerInterval * 1000;

                this._queueName = Program.Config.GetValue<string>($"appSettings:{system}:serviceBrokerQueueName");

                var tableNameOut = Program.Config.GetValue<string>($"appSettings:{system}:messageOutTable");
                var tableNameIn = Program.Config.GetValue<string>($"appSettings:{system}:messageInTable");

                _sqlDependencyOut = new IntegrationService.Domain.SqlDependencyEx(
                    this._connectionString,
                    _databaseName,
                    tableNameOut,
                    "Integration",
                    listenerType: Domain.SqlDependencyEx.NotificationTypes.Insert | Domain.SqlDependencyEx.NotificationTypes.Update,
                    identity: order
                    );

                //NOTE: Input Triggers only on insert
                _sqlDependencyIn = new IntegrationService.Domain.SqlDependencyEx(
                    this._connectionString,
                    _databaseName,
                    tableNameIn,
                    "Integration",
                    listenerType: Domain.SqlDependencyEx.NotificationTypes.Insert,
                    identity: 2
                    );

                _t.Start();

                _log.Info($"MESIntegrationService starting.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to read configuration");
                //throw;
            }
        }

        /// <summary>
        /// PB
        /// This Event handler invocates when ever file created/added in (Default) folder
        /// It sorts the files ordered by file creation timestamp and move 100 files/iteration
        /// </summary>
        /// <param name="sender">File that was created recently in Data_Feed</param>
        /// <param name="e">FileSystemEventArgs by the sender</param>
        private void fswDataFeedCreated(object sender, System.IO.FileSystemEventArgs e)
        {

            try
            {
                _log.Debug("File watcher started"+ ((System.IO.FileSystemWatcher)sender).Path + @"\");
                if (_dataFeedRunning)
                    _dataFeedReRun = true; // We need to again run the DataFeed transfer because some chance files will come while service is running   
                #region EAM INCOMING
                if (!_dataFeedRunning)
                {
                    _dataFeedRunning = true;
                    string[] localIncomingFiles = null;
                    string incomingFilePath = ((System.IO.FileSystemWatcher)sender).Path + @"\";
                    localIncomingFiles = Directory.GetFiles(incomingFilePath);
                    eamInFromCurrentQuantum.Clear();    //Clean up old session content
                                                        //if files where found in the local directory and no error occured start processing
                    if (localIncomingFiles != null)
                    {
                        //for each file found
                        foreach (string file in localIncomingFiles)
                        {
                            //move file to processing directory

                            IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(file), incomingFilePath + @"\", localDirectory + @"\processing\");

                            Models.MaximoAssetData mr = new Models.MaximoAssetData();
                            _log.Debug("Processing XML file (EAMIN): " + Path.GetFileName(file));
                            mr = IntegrationService.Helpers.ExtractMaximoObjectXMLFile.parseMaximoXML(localDirectory + @"\Processing\" + Path.GetFileName(file), _log);
                            if (mr != null)
                            {
                                eamInFromCurrentQuantum.Add(mr);
                                IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(file), localDirectory + @"\processing\", localDirectory + @"\processed\");
                            }
                            else
                            {
                                IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(file), localDirectory + @"\processing\", localDirectory + @"\Rejected\");
                            }

                        }
                    }

                    foreach (Models.MaximoAssetData report in eamInFromCurrentQuantum)
                    {
                        _log.Debug("Transforming XML to MaximoAssetData and MaximoMeterData objects: " + report.AssetNum + " : " + report.filename);
                        WriteToDBLog(report.AssetNum, report.filename, "9999", "Information");
                        if (report.MeterData.Count > 0)
                        {
                            foreach (Models.MaximoMeterData meterdata in report.MeterData)
                            {

                                InsertMaximoImportToMES(report, meterdata);
                                InsertEntryToImportSB(report, meterdata);
                                _log.Debug("EAM SB entry for Asset data and Meter data: " + report.AssetNum + " : " + report.filename + "MeterName" + ":" + meterdata.MeterName.ToString() + "MeterRow" + ":" + meterdata.MeterRow.ToString());
                                WriteToDBLog(report.AssetNum, "WithMeterData:" + meterdata.MeterName + ":" + meterdata.MeterRow, "9999", "Information");
                            }
                        }
                        else
                        {
                            Models.MaximoMeterData meterData = new Models.MaximoMeterData
                            {
                                MeterName = "",
                                MeterRow = 0,
                                Frequency = "",
                                UnitsToGo = ""
                            };

                            InsertMaximoImportToMES(report, meterData);
                            InsertEntryToImportSB(report, meterData);
                            _log.Debug("EAM SB entry (EAMIN) withoutMeterData: " + report.AssetNum + " : " + report.filename + "MeterName" + ":" + meterData.MeterName.ToString() + "MeterRow" + ":" + meterData.MeterRow.ToString());

                        }

                        _log.Debug("Moving XML to  (EAMIN-Processed): " + report.AssetNum + " : " + report.filename);
                        WriteToDBLog(report.AssetNum, "Without MeterData", "9999", "Information");
                    }
                    _dataFeedRunning = false;
                    if (_dataFeedReRun)
                    {
                        _dataFeedReRun = false;
                        fswDataFeedCreated(sender, e);
                    }

                    #endregion EAM INCOMING
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Fatal error");
            }
            finally
            {
                _dataFeedRunning = false;
                eamInTimer.Start();
            }

        }


        private void _t_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                ReadNewMessages(true);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in timer checking messages.");
            }
            finally
            {
                _t.Start();
            }
        }

        public void Dispose()
        {
            if (null != _sqlDependencyOut)
            {
                _sqlDependencyOut.Stop();
            }
            if (null != _sqlDependencyIn)
            {
                _sqlDependencyIn.Stop();
            }
            _sqlDependencyOut = null;
            _sqlDependencyIn = null;
        }

        public void StartListener()
        {
            _sqlDependencyOut.TableChanged += this.OnOutTableChange;
            _sqlDependencyOut.Start();

            _sqlDependencyIn.TableChanged += this.OnInTableChange;
            _sqlDependencyIn.Start();
        }

        public void StopListener()
        {
            _sqlDependencyOut.Stop();
            _sqlDependencyIn.Stop();
        }

        private void OnInTableChange(object o, Domain.SqlDependencyEx.TableChangedEventArgs e)
        {
            try
            {
                // TODO
                // New Message Recieved from JDE to a webService.
                // Parse Message using XML Transformation
                // Call "InsertSP" or save to file.
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to handle TableChange on InputTable.");
                _sqlDependencyIn.Stop();
                _sqlDependencyIn.Start();
            }

        }

        private void OnOutTableChange(object o, Domain.SqlDependencyEx.TableChangedEventArgs e)
        {
            try
            {
                XElement xml = e.Data;
                if (e.NotificationType == SqlDependencyEx.NotificationTypes.Insert)
                {
                    ReadNewMessages();
                }
                else if (e.NotificationType == SqlDependencyEx.NotificationTypes.Update)
                {
                    foreach (XElement item in xml.Descendants("MessageStatusId"))
                    {
                        if (item != null && item.Value == "6") // 6 = Resend
                        {
                            ReadNewMessages();
                            break;
                        }
                    }
                }
            }
            catch (Exception eex)
            {
                _log.Error(eex);
                _sqlDependencyOut.Stop();
                _sqlDependencyOut.Start();
            }

        }


        /// <summary>
        /// Function set all messages to process to "In Progress"/"Resending" => Status 4 and 5
        /// </summary>
        private async void ReadNewMessages(bool callFromTimer = false)
        {
            await semaphoreSlim.WaitAsync(); // Synchronize so that both sql broker and timer events do not preempt.

            _log.Debug("Reading New Messages: START");
            try
            {
                using (var db = new SqlConnection(this._connectionString))
                {
                    string sql = @"UPDATE Integration.MessageOut SET MessageStatusId=4 WHERE MessageStatusId = 1;
                                   UPDATE Integration.MessageOut SET MessageStatusId=5 WHERE MessageStatusId = 6;";

                    var affectedRows = db.Execute(sql);
                    db.Close();// closing the connection before clear by GAC.
                    if (affectedRows > 0 && callFromTimer == true)
                    {
                        // If we had rows to process and call came from timer, we probably need to reinitialize broker connection.
                        _log.Info($"Call from timer found {affectedRows}. Reinitializing SQL brokers");

                        try
                        {
                            _sqlDependencyIn.Start();
                            _sqlDependencyOut.Start();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.ToString(), "Error restarting service broker");
                        }
                    }
                }

                await ProcessMessages();
            }
            catch (SqlException e)
            {
                if (e.Message.Contains("Timeout expired"))
                {
                    _log.Error(e, "Unable to Read New Messages due to timeout SQL exception: ");
                    Thread.Sleep(30000); // wait for SQL connection.
                    semaphoreSlim.Release(); // Release the semaphore Slim
                    ReadNewMessages(false);
                }
                else
                {
                    _log.Error(e, "Unable to Read New Messages due to SQL exception: ");

                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to Read New Messages: ");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }



        /// <summary>
        /// Function that process a queue of messages. Calls the specified procedure and performs string manipulation to the template file.
        /// </summary>
        /// <returns>number of processed messages</returns>
        private async Task<int> ProcessMessages()
        {
            int noOfMessages = 0;
            try
            {
                using (var db = new SqlConnection(this._connectionString))
                {
                    var messages = db.Query<IntegrationMessage>("Integration.spIntegrationGetOutgoingMessages",
                    commandType: CommandType.StoredProcedure);
                    List<IntegrationMessage> messageList = mapper.Map<List<IntegrationMessage>>(messages);
                    foreach (IntegrationMessage message in messageList)
                    {
                        noOfMessages += 1;
                        _log.Debug(String.Format("Message Number = {2} . MessageId {0}: Description: {1} ", message.Id.ToString(), message.Description, noOfMessages.ToString()));

                        var parameterResponse = db.Query<IntegrationMessageParameter>(message.DataStoredProcedure, new { messageId = message.Id }, commandType: CommandType.StoredProcedure);
                        List<IntegrationMessageParameter> paramList = mapper.Map<List<IntegrationMessageParameter>>(parameterResponse);

                        string newMessage = message.MessageTemplate;
                        foreach (IntegrationMessageParameter kvp in paramList)
                        {
                            newMessage = newMessage.Replace(string.Concat(_replaceDelimiter, kvp.Name, _replaceDelimiter), kvp.Value);
                        }

                        message.Message = newMessage;
                        if (message.UseWebService)
                        {
                            await CallExternalWebServiceAsync(message.WebServiceAddress, "", message); // TODO Add ApiFolder or detect Path to use!!
                        }
                        else
                        {
                            message.Message = XDocument.Parse(message.Message).ToString(); // Pretty print XML
                            File.WriteAllText(Path.Combine(message.FolderPath, $"{message.SystemName}_{message.Id}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt"), message.Message);
                        }

                        db.Execute(@"Integration.spIntegrationUpdateProcessedMessage", new { messageId = message.Id, message = message.Message }, commandType: CommandType.StoredProcedure);
                    }
                }

                return noOfMessages;
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to Process Messages.");
                return 0;
            }
        }

        //private void EAM_In_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    lock (eamInLock) // Ensure exclusive access to EAMInFromCurrentQuantum
        //    {
        //        try
        //        {
     
        //            #region EAM INCOMING
        //            string[] localIncomingFiles = null;
        //            // Process Production
        //            localIncomingFiles = Directory.GetFiles(localDirectory + @"\MAXIMOIN\Incoming\");
        //            eamInFromCurrentQuantum.Clear();    //Clean up old session content
        //            //if files where found in the local directory and no error occured start processing
        //            if (localIncomingFiles != null)
        //            {
        //                //for each file found
        //                foreach (string file in localIncomingFiles)
        //                {
        //                    //move file to processing directory

        //                    IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(file), localDirectory + @"\MAXIMOIN\Incoming\", localDirectory + @"\MAXIMOCSVIN\Processing\");

        //                    Models.MaximoAssetData mr = new Models.MaximoAssetData();
        //                    _log.Debug("Processing XML file (EAMIN): " + Path.GetFileName(file));
        //                    mr = IntegrationService.Helpers.ExtractMaximoObjectXMLFile.parseMaximoXML(localDirectory + @"\MAXIMOCSVIN\Processing\" + Path.GetFileName(file), _log);
        //                    if (mr != null)
        //                    {
        //                        eamInFromCurrentQuantum.Add(mr);
        //                    }
        //                    else
        //                    {
        //                        IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(file), localDirectory + @"\MAXIMOCSVIN\Processing\", localDirectory + @"\MAXIMOCSVIN\Rejected\");
        //                    }

        //                }
        //            }

        //            foreach (Models.MaximoAssetData report in eamInFromCurrentQuantum)
        //            {

        //                if (report.MeterData.Count > 0)
        //                {
        //                    foreach (Models.MaximoMeterData meterdata in report.MeterData)
        //                    {
        //                        InsertEntryToImportSB(report.FileType.ToString(), report.AssetNum, report.C_MesType, report.IsRunning, report.OrgId,
        //                        report.Parent, report.Status, meterdata.MeterName, meterdata.MeterRow.ToString(), meterdata.UnitsToGo, meterdata.Frequency, report.SiteId, report.C_IsMes);
        //                        _log.Debug("EAM SB entry with Meter data: " + report.AssetNum + " : " + report.filename + "MeterName" + ":" + meterdata.MeterName.ToString() + "MeterRow" + ":" + meterdata.MeterRow.ToString());
        //                    }
        //                }
        //                else
        //                {
        //                    foreach (Models.MaximoMeterData meterdata in report.MeterData)
        //                    {
        //                        InsertEntryToImportSB(report.FileType.ToString(), report.AssetNum, report.C_MesType, report.IsRunning, report.OrgId,
        //                        report.Parent, report.Status, "", "", "", "", report.SiteId, report.C_IsMes);
        //                        _log.Debug("EAM SB entry (EAMIN) withoutMeterData: " + report.AssetNum + " : " + report.filename + "MeterName" + ":" + meterdata.MeterName.ToString() + "MeterRow" + ":" + meterdata.MeterRow.ToString());
        //                    }
        //                }

        //                _log.Debug("Transforming XML to XML file (EAMIN): " + report.AssetNum + " : " + report.filename);
        //                //processor.createXML(report, config.getLocalMESOUT());

        //                // call to send an entry into the MessagesIN table

        //                //IntegrationService.Helpers.XMLtoCSVTransformation.createMaximoCSV(report, localDirectory + @"\MAXIMOCSVIN\Processing\", _log);

        //                _log.Debug("Moving CSV to Incoming folders (EAMIN-Incoming): " + report.AssetNum + " : " + report.filename);
        //                //Move file that was processed
        //                //IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(report.filename), localDirectory + @"\MAXIMOCSVIN\Processing\", localDirectory + @"\MAXIMOCSVIN\Processed\");
        //                //Move new file to outgoing
        //                //IntegrationService.Helpers.FileProcessing.moveFile(Path.GetFileName(report.filename) + ".csv", localDirectory + @"\MAXIMOCSVIN\Processing\", remoteDirectory + @"\MAXIMOCSVIN\Incoming\");
        //            }
        //            #endregion EAM INCOMING

        //        }
        //        catch (Exception ex)
        //        {
        //            _log.Error(ex,"Fatal error");
        //        }
        //        finally
        //        {
        //            eamInTimer.Start();
        //        }
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseURI">The webserver address. E.g: http://10.10.10:6005</param>
        /// <param name="apiName">The address to the API. E.g : /api/NewMessage</param>
        /// <param name="message">an IntegrsyncationMessage</param>
        private async Task CallExternalWebServiceAsync(string baseURI, string apiName, IntegrationMessage message)
        {
            try
            {
                var sendUri = String.Format(apiName);
                System.Net.Http.HttpResponseMessage responseMessage = null;

                using (System.Net.Http.HttpClient externalWebService = new System.Net.Http.HttpClient())
                {
                    Uri _baseURI = new Uri(baseURI);
                    externalWebService.BaseAddress = _baseURI;
                    System.Net.Http.StringContent content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(message.Message), System.Text.Encoding.UTF8, "application/json");

                    responseMessage = await externalWebService.PostAsync(sendUri, content);
                    if (responseMessage.IsSuccessStatusCode)
                    {
                    }
                    else
                    {
                        WriteToDBLog(message.BatchId, responseMessage.ReasonPhrase, message.ProductionUnitId, "Error");
                        _log.Error("Unable to send to External WebService. StatusCode:" + responseMessage.StatusCode + " Reason:" + responseMessage.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to send to External WebService");
            }
        }

        /// <summary>
        /// Log information to the EventLog in the MES database
        /// </summary>
        /// <param name="batchId">The batchID</param>
        /// <param name="message">The message to store. Max 2048 characters</param>
        /// <param name="ProductionUnit"> the productionUnit</param>
        /// <param name="level">Informational, Error or Warning</param>
        private void WriteToDBLog(string batchId, string message, string ProductionUnit = "", string level = "Informational")
        {
            try
            {
                using (var db = new SqlConnection(this._connectionString))
                {
                    db.Execute(@"Integration.spLocal_Insert_LogMessageEvent",
                        new
                        {
                            ProcessArea = "IntegrationService",
                            ProcessOrder = batchId,
                            Source = ProductionUnit,
                            Desc = message,
                            ErrorMsg = level
                        },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to log to EventTable in database.");
            }
        }
       /// <summary>
       /// This function extracts to data from the Maximoassetdata and Meterdata objects and execute a procedure call 
       /// imports the data into MES - SOADB tables
       /// </summary>
       /// <param name="report">MaximoAssetData Object</param>
       /// <param name="meterData">MaximoMeterData Object</param>
        private void InsertMaximoImportToMES(Models.MaximoAssetData report, Models.MaximoMeterData meterData)
        {
            try
            {
                using (var db = new SqlConnection(this._connectionString))
                {
                    var meterRow = meterData.MeterRow.ToString();
                    if (meterRow == "0")
                        meterRow = "";

                    using (SqlCommand cmd = db.CreateCommand())
                    {
                        cmd.CommandText = "[SOADB].[dbo].[spLocal_Import_MAXIMO_MES_Test]";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_Id", 0);
                        cmd.Parameters.AddWithValue("EC_Id", 59);
                        cmd.Parameters.AddWithValue("FILETYPE", report.FileType.ToString());
                        cmd.Parameters.AddWithValue("ASSETNUM", report.AssetNum);
                        cmd.Parameters.AddWithValue("SITEID", string.IsNullOrEmpty(report.SiteId.ToString()) ? "" : report.SiteId.ToString());
                        cmd.Parameters.AddWithValue("C_ISMES", string.IsNullOrEmpty(report.C_IsMes.ToString()) ? "" : report.C_IsMes.ToString());
                        cmd.Parameters.AddWithValue("C_MESTYPE", report.C_MesType);
                        cmd.Parameters.AddWithValue("ISRUNNING", report.IsRunning);
                        cmd.Parameters.AddWithValue("ORGID", report.OrgId);
                        cmd.Parameters.AddWithValue("PARENT", report.Parent);
                        cmd.Parameters.AddWithValue("STATUS", report.Status);
                        cmd.Parameters.AddWithValue("METERNAME", meterData.MeterName);
                        cmd.Parameters.AddWithValue("METERROW",  meterRow);
                        cmd.Parameters.AddWithValue("UNITSTOGO", meterData.UnitsToGo);
                        cmd.Parameters.AddWithValue("FREQUENCY", meterData.Frequency);
                        var success = new SqlParameter("Success", SqlDbType.VarChar,10);
                        var errorMSG = new SqlParameter("ErrorMsg", SqlDbType.VarChar, 255);
                        success.Direction = ParameterDirection.Output;
                        errorMSG.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(success);
                        cmd.Parameters.Add(errorMSG);
                        db.Open();
                        var returnvalue = cmd.ExecuteNonQuery();
                        var result = success.Value.ToString();
                        var result1 = errorMSG.Value.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to insert entry to EAM to MES Import SB.");
            }
        }

        /// <summary>
        /// This function extracts the data from MaximoAssetData and MaximoMeterData objects and execute
        /// and ineserts a row into service broker queue.
        /// </summary>
        /// <param name="report">MaximoAssetData object</param>
        /// <param name="meterData">MaximoMeterData object</param>
        private void InsertEntryToImportSB(Models.MaximoAssetData report, Models.MaximoMeterData meterData)
        {
            try
            {
                using (var db = new SqlConnection(this._connectionString))
                {
                    _ = db.Execute(@"[ChainDB].[dbo].[sMES_SendEAMToMESEntry_SB]",
                        new
                        {
                            MSGTYPE = "204",
                            MSGDESCRIPTION = "Maximo_Import",
                            BATCHID = report.AssetNum,
                            LOCATION = "1",
                            USERID = "H979340",
                            @PARAM01 = "59",
                            @PARAM01DESC = "EC_Id",
                            @PARAM02 = report.FileType.ToString(),
                            @PARAM02DESC = "FILTETYPE",
                            @PARAM03 = report.AssetNum,
                            @PARAM03DESC = "PARENTI",
                            @PARAM04 = string.IsNullOrEmpty(report.SiteId.ToString()) ? "" : report.SiteId.ToString(),
                            @PARAM04DESC = "SITEID",
                            @PARAM05 = string.IsNullOrEmpty(report.C_IsMes.ToString()) ? "" : report.C_IsMes.ToString(),
                            @PARAM05DESC = "C_ISMES",
                            @PARAM06 = report.C_MesType,
                            @PARAM06DESC = "C_MESTYPE",
                            @PARAM07 = report.IsRunning,
                            @PARAM07DESC = "ISRUNNING",
                            @PARAM08 = report.OrgId,
                            @PARAM08DESC = "ORGID",
                            @PARAM09 = report.Parent,
                            @PARAM09DESC = "PARENT",
                            @PARAM10 = report.Status,
                            @PARAM10DESC = "STATUS",
                            @PARAM11 = meterData.MeterName,
                            @PARAM11DESC = "METERNAME",
                            @PARAM12 = meterData.MeterRow,
                            @PARAM12DESC = "METERROW",
                            @PARAM13 = meterData.UnitsToGo,
                            @PARAM13DESC = "UNITSTOGO",
                            @PARAM14 = meterData.Frequency,
                            @PARAM14DESC = "FREQUENCY"

                        },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to insert entry to EAM to MES Import SB.");
            }
        }







    }
}
