using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace CollegeFootballOddsScraper
{
    public class DataParser
    {

        private string FeedMode;
        private string provider;
        private HtmlDocument htmlData;
        private XDocument xmlData;
        private bool abbreviatedMonth = false;
        //private SyndicationFeed rssData;
        private readonly string nodata = "No Data Available";

        private Dictionary<string, string> GameInfo = new Dictionary<string, string>
        {
            {   "StartTime",                ""},
            {   "StartYear",                ""},
            {   "StartMonth",               ""},
            {   "StartDay",                 ""},
            {   "StartUnixTime",            ""}, //this is best to get where possible - absolute value can be converted to any time zone.
            {   "StartTimeMilitary",        ""},
            {   "StartFormatted",           ""},
            {   "StartFullDate",           ""},
            {   "Broadcaster",              ""},
            {   "HomeTeamName",             ""},
            {   "HomeTeamLogoSource",       ""},
            {   "HomeTeamOdds",             ""},
            {   "VisitingTeamName",         ""},
            {   "VisitingTeamLogoSource",   ""},
            {   "VisitingTeamOdds",         ""},
            {   "GameOddsType",             ""},
            {   "GameSpread",              ""},
            { "Underdog", ""},
            { "Favorite", ""}
        };

        private Dictionary<string, string> HTMLNodeMap = new Dictionary<string, string>
        {
            {"BaseGamesXpathSelector",          ""},
            { "StartTimeXpathSelector", ""},
            {"TeamLogoXpathSelector",           ""},
            {"TeamNameXpathSelector",           ""},
            {"VisitingTeamNameXpathSelector",   ""},
            {"HomeTeamNameXpathSelector",       ""},
            {"GameInfoXpathSelector",           ""},
            {"OddsTypeXpathSelector",           ""},
            {"LineOddsXpathSelector",           ""},
            {"SpreadOddsXpathSelector",         ""},
            {"HomeSpreadOddsXpathSelector",     ""},
            {"DateOfGameXpathSelector",         ""},
            { "BothTeamsXpathSelector", ""},
            {"GamesByDayXpathSelector", "" }
        };

        private Dictionary<string, string> DataSources = new Dictionary<string, string>
        {
            {"pinnacle" ,   "http://xml.pinnaclesports.com/pinnaclefeed.aspx?sporttype=Football&sportsubtype=ncaa" },
            {"5Dimes",      "http://stats.5dimes.eu/Stats/Matchups/Football/NCAAF/999" },
            {"bookmaker",   "http://www.bookmaker.eu/live-lines/football/college-football" }
          
        };


        #region Class Constructors

        //public DataParser(HtmlDocument rawHTML)
        //{
        //    feedMode = "HTML";
        //    htmlData = rawHTML;
        //}

        //public DataParser(XDocument rawXML)
        //{
        //    feedMode = "XML";
        //    xmlData = rawXML;
        //}

        public DataParser(string feedname, string feedMode)
        {
            FeedMode = feedMode;
            provider = feedname;

            switch (feedMode)
            {
                case "XML":
                    var feed = DataSources[provider.ToString()];
                    xmlData = XDocument.Load(DataSources[provider]);
                    break;
                case "HTML":
                    htmlData = (new HtmlWeb()).Load(DataSources[provider]);
                    break;
            }
        }

        //public DataParser(SyndicationFeed rssFeed)
        //{
        //    feedMode = "Rss";
        //    rssData = rssFeed;
        //}

        #endregion

        #region public methods

        public List<TableRow> GetScoresInTableFormat()
        {
            switch (FeedMode)
            {
                case "HTML":
                    return HTMLDataTableFormatter();
                case "XML":
                    return XMLTableFormatter();
                //case "RSS":
                //     break;
                default:
                    return new List<TableRow>();
            }
        }

        #endregion

        #region helper methods

        #region html specific parse logic

        private List<TableRow> HTMLDataTableFormatter()
        {
            BuildHTMLMap();

            if (HTMLNodeMap["GamesByDayXpathSelector"] == "")
            {
                return BuildGameInfoTableRows(htmlData.DocumentNode.SelectNodes(HTMLNodeMap["BaseGamesXpathSelector"]));
            }

            var tablerows = new List<TableRow>();
            foreach (var gameday in htmlData.DocumentNode.SelectNodes(HTMLNodeMap["GamesByDayXpathSelector"]))
            {
                GameInfo["StartFullDate"] = Clean(gameday.SelectSingleNode(HTMLNodeMap["DateOfGameXpathSelector"]).InnerText);
                tablerows.AddRange(BuildGameInfoTableRows(gameday.SelectNodes(HTMLNodeMap["BaseGamesXpathSelector"])));
            }
            return tablerows;
        }

        private List<TableRow> BuildGameInfoTableRows(HtmlNodeCollection matchups)
        {
            var tablerows = new List<TableRow>();
            foreach (var matchup in matchups)
            {
                if (matchup.Attributes["class"].Value == "Column_Headers")
                {
                    GameInfo["StartFullDate"] = Clean(matchup.SelectSingleNode(HTMLNodeMap["DateOfGameXpathSelector"]).InnerText);
                }
                else
                {
                    LoadGameInfoDictionary(matchup);
                    //var x = matchup.SelectSingleNode(HTMLNodeMap["StartTimeXpathSelector"]).InnerText;
                    var militaryTime = ConvertStarttimeToMilitary(matchup.SelectSingleNode(HTMLNodeMap["StartTimeXpathSelector"]).InnerText);
                    var startDateTime = GetFormattedDate(militaryTime[0], militaryTime[1]);
                    tablerows.Add(
                      new TableRow
                      {
                          Cells = {
                            new TableCell   {   Text = startDateTime.ToString()},
                            new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["Favorite"])        ? nodata : GameInfo["Favorite"]) },
                            new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["GameSpread"])          ? nodata : GameInfo["GameSpread"]) },
                            new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["Underdog"])      ? nodata : GameInfo["Underdog"]) },
                            //new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["VisitingTeamLogoSource"])? nodata : "<img src='" + GameInfo["VisitingTeamLogoSource"] + "' />") },
                            //new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["VisitingTeamOdds"])      ? nodata : GameInfo["VisitingTeamOdds"]) },
                            //new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["HomeTeamName"])          ? nodata : GameInfo["HomeTeamName"]) },
                            //new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["HomeTeamLogoSource"])    ? nodata : "<img src='" + GameInfo["HomeTeamLogoSource"] + "' />") },
                            //new TableCell   {   Text = (string.IsNullOrEmpty(GameInfo["HomeTeamOdds"])          ? nodata : GameInfo["HomeTeamOdds"]) }
                              }
                      });
                }
              
            }
            return tablerows;
        } 


        private void BuildHTMLMap()
        {
            switch (provider)
            {
                case "cbs":     //working
                    HTMLNodeMap["BaseGamesXpathSelector"] = "//table[contains(@class,'preEvent')]";
                    HTMLNodeMap["TeamLogoXpathSelector"] = "td/a/img";
                    HTMLNodeMap["TeamNameXpathSelector"] = "td/div/a";
                    HTMLNodeMap["GameTimeXpathSelector"] = "td/span/span";
                    HTMLNodeMap["LineOddsXpathSelector"] = "td[contains(@class,'gameOdds')]";
                    HTMLNodeMap["OddsTypeXpathSelector"] = "td[contains(@class,'gameOdds')]";
                    break;
                case "official": //still in development (jason)
                    HTMLNodeMap["BaseGamesXpathSelector"] = "//div[contains(@class, 'game-contents')]";
                    HTMLNodeMap["TeamLogoXpathSelector"] = "//td[contains(@class, 'school')]";
                    HTMLNodeMap["TeamNameXpathSelector"] = "//div[contains(@class, 'team')]/a";
                    HTMLNodeMap["GameTimeXpathSelector"] = "//div[contains(@class, 'game-status')]";
                    //HTMLNodeMap["LineOddsXpathSelector"] = "td[contains(@class,'gameOdds')]";
                    //HTMLNodeMap["OddsTypeXpathSelector"] = "td[contains(@class,'gameOdds')]";
                    break;
                case "5Dimes":
                    HTMLNodeMap["BaseGamesXpathSelector"]    = @"//table[contains(@id, 'chalk')][2]/tr";
                    HTMLNodeMap["DateOfGameXpathSelector"]  = "td";
                    HTMLNodeMap["BothTeamsXpathSelector"]   = "td[2]";
                    HTMLNodeMap["SpreadOddsXpathSelector"]  = "td[3]";
                    HTMLNodeMap["StartTimeXpathSelector"]   = "td[1]";
                    break;
                case "bookmaker":
                    HTMLNodeMap["BaseGamesXpathSelector"]       = "div[contains(@class, 'matchup')]";
                    HTMLNodeMap["GamesByDayXpathSelector"]      = "//div[contains(@class, 'externalLinesPage')]";
                    HTMLNodeMap["DateOfGameXpathSelector"]      = "div/span";
                    HTMLNodeMap["StartTimeXpathSelector"]       = ".//li[contains(@class, 'time')]/span";
                    HTMLNodeMap["HomeTeamXpathSelector"]        = ".//div[contains(@class, 'hTeam')]/div/h3/span";
                    HTMLNodeMap["VisitingTeamXpathSelector"]    = ".//div[contains(@class, 'vTeam')]/div/h3/span";
                    HTMLNodeMap["SpreadOddsXpathSelector"]      = ".//div[contains(@class, 'hTeam')]/div[contains(@class, 'spread')]";
                    abbreviatedMonth = true;
                    break;
            }
        }

        private void LoadGameInfoDictionary(HtmlNode gameTable)
        {
            
            if (HTMLNodeMap["BothTeamsXpathSelector"] != "")
            {
                var teams = Regex.Split(gameTable.SelectSingleNode(HTMLNodeMap["BothTeamsXpathSelector"]).InnerText, " vs. ");
                GameInfo["HomeTeamName"] = "*" + Clean(teams[1]).ToUpper() + "*";
                GameInfo["VisitingTeamName"] = Clean(teams[0]).ToLower();
            }
            else
            {
                GameInfo["HomeTeamName"] = "*" + Clean(gameTable.SelectSingleNode(HTMLNodeMap["HomeTeamXpathSelector"]).InnerText).ToUpper() + "*";
                GameInfo["VisitingTeamName"] = Clean(gameTable.SelectSingleNode(HTMLNodeMap["VisitingTeamXpathSelector"]).InnerText.ToLower());
            }
            GameInfo["GameSpread"]                  = Clean(gameTable.SelectSingleNode(HTMLNodeMap["SpreadOddsXpathSelector"]).InnerText) ;
                
                SetFavoriteAndOdds();
        }

        private DateTime GetFormattedDate(int hour, int minutes)
        {
            var date = abbreviatedMonth ? Clean(GameInfo["StartFullDate"].Replace("COLLEGE FOOTBALL - ", "")) : Clean(GameInfo["StartFullDate"]) ;
            GameInfo["StartFullDate"] = date;
            var dateparts = Regex.Split(date, "[ ]+");

            var month = MonthNameToNumeric(dateparts[0]);
            var day = int.Parse(dateparts[1].Replace(",", ""));
            var year = 2015;//int.Parse(dateparts[2]);
            
            return new DateTime(year, month, day, hour == -1 ?0 : hour, minutes == -1 ? 0: minutes, 0);
        }

        private int MonthNameToNumeric(string month)
        {
            if (abbreviatedMonth)
            {
                var monthNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
                return Array.IndexOf(monthNames, month) + 1;
            }
            return DateTime.ParseExact(month, "MMMM", CultureInfo.CurrentCulture).Month;
        }
        private void SetFavoriteAndOdds()
        {
            var spread = GameInfo["GameSpread"];
            

            var homeIsFavorite = (spread.Equals("")) ? false : spread.Substring(0, 1).Equals("-");
            
            if (homeIsFavorite)
            {
                GameInfo["GameSpread"] = spread.Equals("") ? "odds unavailable": Clean(spread.Split('-')[1]);
                GameInfo["Underdog"] =
                    Clean(GameInfo["VisitingTeamName"]);
                GameInfo["Favorite"] =
                    Clean(GameInfo["HomeTeamName"]);

            }
            else
            {
                GameInfo["GameSpread"] = spread.Equals("") ? "odds unavailable" : Clean(spread.Split('-')[0]);
                GameInfo["Underdog"] =
                   Clean(GameInfo["HomeTeamName"]);
                GameInfo["Favorite"] =
                    Clean(GameInfo["VisitingTeamName"]);
                
            }
        }

        private void AddTeamRowDataToDictionary(HtmlNode teamData, string teamType)
        {
            GameInfo[teamType + "Name"] = teamData.SelectSingleNode(HTMLNodeMap["TeamNameXpathSelector"]).InnerText;
            GameInfo[teamType + "Odds"] = teamData.SelectSingleNode(HTMLNodeMap["LineOddsXpathSelector"]).InnerText;
            var image = teamData.SelectSingleNode(HTMLNodeMap["TeamLogoXpathSelector"]);
            GameInfo[teamType + "LogoSource"] = image.GetAttributeValue("delaysrc", "no source");
        }

        private void AddGameRowDataToDictionary(HtmlNode gameData)
        {
            //var gameTimeNode = gameData.SelectSingleNode(HTMLNodeMap["GameTimeXpathSelector"]);

            GameInfo["StartUnixTime"] = gameData.SelectSingleNode(HTMLNodeMap["GameTimeXpathSelector"]).GetAttributeValue("data-gmt", "no start time info");
            GameInfo["StartFormatted"] = gameData.SelectSingleNode(HTMLNodeMap["GameTimeXpathSelector"]).InnerText;
            GameInfo["GameOddsType"] = gameData.SelectSingleNode(HTMLNodeMap["OddsTypeXpathSelector"]).InnerText;
        }

        #endregion

        #region xml specific parse logic

        private List<TableRow> XMLTableFormatter()//so far this is provider specific, will create more generic logic if/when a viable secondary option is found.
        {
            var tablerows = new List<TableRow>();
            foreach (var game in xmlData.Descendants("event"))
            {
                GameInfo["VisitingTeamName"]    = game.Descendants("visiting_home_draw").Where(team => team.Value == "Visiting").Select(element => element.Parent).Descendants("participant_name").FirstOrDefault().Value.ToLower();
                GameInfo["HomeTeamName"]        = "*" +  game.Descendants("visiting_home_draw").Where(team => team.Value == "Home").Select(element => element.Parent).Descendants("participant_name").FirstOrDefault().Value.ToUpper() + "*";
                GameInfo["GameSpread"]          = game.Descendants("spread_home").FirstOrDefault().Value;
                SetFavoriteAndOdds();
                tablerows.Add(
                   new TableRow
                   {
                       Cells = {

                            new TableCell   {   Text = game.Descendants("event_datetimeGMT").FirstOrDefault().ToString() },
                            new TableCell   {   Text = GameInfo["Favorite"]},
                            new TableCell   {   Text = GameInfo["GameSpread"]},
                            new TableCell   {   Text = GameInfo["Underdog"]}                            
                        }
                   }
                );
              }
            return tablerows;
        }
        private int[] ConvertStarttimeToMilitary(string starttime)
        {
            if (starttime.Equals("TBA"))
                return new[] { -1, -1 };

            var dateparts = Clean(starttime).Split(' ');
            var hourAndMinutes = dateparts[0].Split(':');
            var hour = ToCentralTime(hourAndMinutes[0], Clean(dateparts[1].ToLower()).Equals("pm")) ;
            var minutes = int.Parse(hourAndMinutes[1]);

            return new[] { hour, minutes };
        }
        private  int ToCentralTime(string stringHour, bool isEveningGame)
        {
            var hourModifier = isEveningGame ? 15 : 3;
            var intHour = int.Parse(stringHour);
            var CSTHour = intHour != 12 ? intHour + hourModifier : hourModifier;
            if (CSTHour < 24)
            {
                return CSTHour;
            }
            CSTHour -= 24;
            return CSTHour;

        }
        private string Clean( string dirty)
        {
            return dirty.Replace("&amp;", "&").Replace("+", "").Replace("\r\n", "").Replace("\r", "").Replace("\n", "").Replace("&frac12;", ".5").Replace("(", "").Replace(")", "").Trim();
        }
        #endregion

        #endregion

        // CBS OUTER GAME OBJECT
        //
        // <table class="lineScore preEvent">
        //    <tr class="gameInfo">
        //        <td>
        //            <span class="gameDate"> <span class="gmtTime" data-gmt="1441481400" data-gmt-format="%r  %q %e - %I:%M %p %Z">Sat.  Sept. 5 - 3:30 PM EDT</span>  (ABC) </span>
        //        </td>
        //      <td class="gameOdds">Line</td>
        //  </tr>
        //  <tr class="teamInfo awayTeam" >
        //      <td class="teamName">
        //        <a href="/collegefootball/teams/page/BYU/brigham-young-cougars">
        //            <img delaysrc="http://sports.cbsimg.net/images/collegefootball/logos/25x25/BYU.png" width="25" height="25" border="0" class="teamLogo" />
        //        </a>
        //        <div class="teamLocation">
        //                <a href="/collegefootball/teams/page/BYU/brigham-young-cougars">BYU</a>
        //        </div>
        //      </td>
        //      <td class="gameOdds">-</td>
        //    </tr>
        //    <tr class="teamInfo homeTeam">
        //        <td class="teamName">
        //            <a href="/collegefootball/teams/page/NEB/nebraska-cornhuskers" />
        //                <img delaysrc="http://sports.cbsimg.net/images/collegefootball/logos/25x25/NEB.png" width="25" height="25" border="0" class="teamLogo"></a>
        //                <div class="teamLocation">
        //                    <a href="/collegefootball/teams/page/NEB/nebraska-cornhuskers">Nebraska</a>
        //                </div>
        //        </td>
        //        <td class="gameOdds">-</td>
        //    </tr>
        //</table>



    }
}