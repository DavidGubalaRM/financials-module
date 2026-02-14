using MCase.Core.Event;
using MCaseCustomEvents.CommonUtilities;
using MCaseEventsSDK;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using static MCase.Event.NMImpact.Constants.NMFinancialConstants;

namespace MCase.Event.NMImpact.Financials
{
    public class FinanceServices
    {

        public static async Task<EventReturnObject> MakePostRestCall(object dataObject, AEventHelper eventHelper)
        {

            var accountingEndPoint = WebConfigurationManager.AppSettings[AccountingAPI.EndPoint];
            var accountingEndPointToken = WebConfigurationManager.AppSettings[AccountingAPI.EndPointToken];

            var url = accountingEndPoint;

            //token is only needed for deployed environments, so only add to url is populated
            if (!string.IsNullOrEmpty(accountingEndPointToken))
                url = accountingEndPoint + @"?code=" + accountingEndPointToken;

            eventHelper.AddDebugLog("FinanceServices: Make Finance Post Rest call, url = " + url);

            string json = string.Empty;
            var returnValue = new EventReturnObject();

            try
            {
                json = JsonConvert.SerializeObject(dataObject);
            }
            catch (Exception ex)
            {
                eventHelper.AddDebugLog($"FinanceServices: Exception at Finance Make Post Rest Call: {ex.Message}");

            }
            var httpClient = HttpClientService.Instance.GetHttpClient();
            HttpResponseMessage response = null;

            HttpContent requestBody = new StringContent(string.Empty);

            requestBody = new StringContent(json, Encoding.UTF8, "application/json");
            UriBuilder baseUri = new UriBuilder(url);

            eventHelper.AddDebugLog($"FinanceServices: Make Finance Post Rest call, url: {baseUri}, payload: {json}");
            try
            {
                response = await httpClient.PostAsync(baseUri.Uri, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var responseCode = response.StatusCode;
                    var responseJson = await response.Content.ReadAsStringAsync();
                    eventHelper.AddDebugLog($"FinanceServices: Unexpected http response {responseCode}: {responseJson}");
                    throw new Exception($"Unexpected http response {responseCode}: {responseJson}");
                }

                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var responseText = await response.Content.ReadAsStringAsync();

                returnValue.Status = EventStatusCode.Success;
                returnValue.AddMessage(responseText);

            }
            catch (Exception ex)
            {
                if (response != null)
                    returnValue.AddError(response.ReasonPhrase);
                eventHelper.AddDebugLog("FinanceServices: exception calling post async" + ex.Message);
                returnValue.AddError(ex.Message);
            }

            return await Task.FromResult(returnValue);
        }

        /// <summary>
        /// object to deserialize the data into - taken from CSSUtils.cs
        /// </summary>
        public class ApiGetResponse
        {
            public HttpStatusCode StatusCode { get; set; }
            public string Message { get; set; }
            public string Data { get; set; }
        }


    }
}