using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace CollegeFootballOddsScraper
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void btnScores_Click(object sender, EventArgs e)
        {
            HideAllTables();
            ActivateTable(((Button)sender).CommandName);

            var commands = ((Button) sender).CommandArgument.Split(',');
            switch (commands[0]) //commandargument of the calling button tells which type of doc to parse
            {
                case "HTML":
                    //fetch html
                    //http://www.oddsshark.com/ncaaf/odds
                    //var htmlData = (new HtmlWeb()).Load("http://www.ncaa.com/scoreboard/football/fbs");                    
                    //var htmlData = (new HtmlWeb()).Load("http://www.cbssports.com/collegefootball/scoreboard");
                   
                    (new DataParser(commands[1], commands[0])).GetScoresInTableFormat().ForEach(row => tblHTMLData.Rows.Add(row));

                    break;
                case "XML":
                    //fetch
                  
                    //process
                    (new DataParser(commands[1], commands[0])).GetScoresInTableFormat().ForEach(row => tblXMLData.Rows.Add(row));
                    break;
                case "RSS":
                    //this is unplugged, waiting on a suitable feed for development
                    var xFeed = XDocument.Load("http://www.nytimes.com/services/xml/rss/nyt/International.xml");
                   // var feed = SyndicationFeed.Load(xFeed.CreateReader()); //syndication feed object
                    break;
            }
        }

        private void ActivateTable(string tableID)
        {
            ((Table)FindControl(tableID)).Visible = true; //show relevant table (button command name stores the tableID)
        }

        private void HideAllTables()
        {
            foreach (var control in FindControl("allTables").Controls)
            {
                if (control is Table)
                { ((Table)control).Visible = false; }
            }
        }
    }
}