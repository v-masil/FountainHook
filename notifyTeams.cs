#r "Newtonsoft.Json"
using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public static string HtmlToText(string HTMLCode)
{
    // Feature descriptions often have lots of rich text given in HTML, making the card very long.
    // If the card is too long, the card will fail silently and not show up in Teams.
    // This function tries to strip away the really egregious HTML while maintaining readability.

    // Remove new lines since they are not visible in HTML
    HTMLCode = HTMLCode.Replace("\n", " ");
    
    // Remove tab spaces
    HTMLCode = HTMLCode.Replace("\t", " ");
    
    // Remove multiple white spaces from HTML
    HTMLCode = Regex.Replace(HTMLCode, "\\s+", " ");
    
    // Remove any JavaScript
    HTMLCode = Regex.Replace(HTMLCode, "<script.*?</script>", ""
    , RegexOptions.IgnoreCase | RegexOptions.Singleline);
    
    // Replace special characters like &, <, >, " etc.
    StringBuilder sbHTML = new StringBuilder(HTMLCode);
    // Note: There are many more special characters, these are just
    // most common. You can add new characters in this arrays if needed
    string[] OldWords = {"&nbsp;", "&amp;", "&quot;", "&lt;", 
    "&gt;", "&reg;", "&copy;", "&bull;", "&trade;"};
    string[] NewWords = {" ", "&", "\"", "<", ">", "Â®", "Â©", "â€¢", "â„¢"};
    for(int i = 0; i < OldWords.Length; i++)
    {
        sbHTML.Replace(OldWords[i], NewWords[i]);
    }
    
    // Check if there are line breaks (<br>) or paragraph (<p>)
    sbHTML.Replace("<br>", "!!br!!");
    //sbHTML.Replace("<br ", "!!br ");
    sbHTML.Replace("<p ", "!!p ");

    // Replace style-br's with a normal br
    string result = System.Text.RegularExpressions.Regex.Replace(sbHTML.ToString(), "<br style[^>]*>", "!!br!!");
    result = System.Text.RegularExpressions.Regex.Replace(result, "<div style[^>]*>", "!!br!!");
    
    // Remove all <tags>
    result = System.Text.RegularExpressions.Regex.Replace(
    result, "<[^>]*>", "");

    // Sub back in the <br>s and <p>s
    result = result.Replace("!!br!!", "<br>");
    //result = result.Replace("!!br", "<br");
    result = result.Replace("!!p", "<p");

    return result;
}
 
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    // Hooked in VSTS, whenever a feature's ring1_5 or ring3 field is updated.

    bool PRODUCTION = true;

    // Use the right Teams incoming hook
    List<string> TEAMS_HOOK_URIS = new List<string>();
    List<string> TEAMS_INTERNAL_HOOK_URIS = new List<string>();

    List<string> RING_1_5_3_HOOK_URIS = new List<string>();
    List<string> RING_4_HOOK_URIS = new List<string>();

    List<string> RING_1_5_3_INTERNAL_HOOK_URIS = new List<string>();
    List<string> RING_4_INTERNAL_HOOK_URIS = new List<string>();
    List<string> RING_1_INTERNAL_HOOK_URIS = new List<string>();
    List<string> RING_2_INTERNAL_HOOK_URIS = new List<string>();
    List<string> RING_3_9_INTERNAL_HOOK_URIS = new List<string>();

    string FIELD_NAMESPACE, VALIDATION_FIELD_NAMESPACE, PROJECT_NAME, PASS_FAIL_URI, RATING_URI;
    string SUB_ID_1_5, SUB_ID_3, SUB_ID_4;
    string SUB_ID_1, SUB_ID_2, SUB_ID_3_9;

    bool FEEDBACK_DISABLED;
    if (PRODUCTION) {
        // Really really production channel
        RING_1_5_3_HOOK_URIS.Add("https://outlook.office.com/webhook/6d896def-4cb5-4c5d-99d4-3b7644211e35@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/9adde8f16cde469fbf1cb6b48ba60d6d/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");
        // Really really Skype production channel
        RING_1_5_3_HOOK_URIS.Add("https://outlook.office.com/webhook/83d28c15-f89c-435d-bba2-a328b476203d@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/0b001b5abda6419a8bd16ad9a61c129c/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");

        // TAP100 R4 channel
        RING_4_HOOK_URIS.Add("https://outlook.office.com/webhook/6d896def-4cb5-4c5d-99d4-3b7644211e35@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/73d08992f9844fc884f652bb6cc88307/512d26c9-aeed-4dbd-a16f-398bcf0ec3fe");
        // Skype R4 channel
        RING_4_HOOK_URIS.Add("https://outlook.office.com/webhook/eb655d71-4d80-444b-8e54-86aa39f217af@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/59d067e15f944b59ac2076943b948cf0/f3242314-a95b-4fb1-89d9-1f9365a66d5b");

        // New webhooks for internal MS users; they get a different card and have more rings to worry about
        RING_1_5_3_INTERNAL_HOOK_URIS.Add("https://outlook.office.com/webhook/0a101af1-cb8a-448f-bfa3-1711337e7419@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/98b91d424e7c42a3b253bfbc078ff0a5/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");
        RING_4_INTERNAL_HOOK_URIS.Add("https://outlook.office.com/webhook/0a101af1-cb8a-448f-bfa3-1711337e7419@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/c0dbcd7213ba4d398bee092b588567a1/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");
        RING_1_INTERNAL_HOOK_URIS.Add("https://outlook.office.com/webhook/0a101af1-cb8a-448f-bfa3-1711337e7419@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/1f1e5dabae4c49d4966441eb55888a80/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");
        RING_2_INTERNAL_HOOK_URIS.Add("https://outlook.office.com/webhook/0a101af1-cb8a-448f-bfa3-1711337e7419@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/ea6e07c0d3164ea5b0a4fa970158069e/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");
        RING_3_9_INTERNAL_HOOK_URIS.Add("https://outlook.office.com/webhook/0a101af1-cb8a-448f-bfa3-1711337e7419@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/aeeb2517cf164dcca3fdc26d490b7df9/57e55a0b-56ad-4108-8319-8d5d9a7ea6fc");

        // Testing TAP channel
        //INCOMING_HOOK_URI = "https://outlook.office.com/webhook/37317ed8-68c1-4564-82bb-d2acc4c6b2b4@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/b4073f0edef34b899ea708a4b6659978/512d26c9-aeed-4dbd-a16f-398bcf0ec3fe";
        // Vance channel
        //INCOMING_HOOK_URI = "https://outlook.office.com/webhook/15f69bfd-b260-478c-af37-db567141623a@240a3177-7696-41df-a4ea-0d1b0999fb38/IncomingWebhook/a79736f03e834b05b66a805574d20cc3/2c46ba10-5141-459c-a866-d47436de0cba";
        FIELD_NAMESPACE = "MicrosoftTeamsCMMI";
        VALIDATION_FIELD_NAMESPACE = "MicrosoftTeamsCMMI-Copy";
        PROJECT_NAME = "MSTeams";
        PASS_FAIL_URI = "https://fountainhookproduction.azurewebsites.net/api/PassFailResponse?code=vrsejDXtAbVnAA6sBpkli0QOPM9PTOkFxvak3Lw9us6pn5S2NePtqw==";
        RATING_URI = null;
        SUB_ID_1_5 = "cefb2308-2c11-4b88-a9d8-4db94270b756";
        SUB_ID_3 = "aeff807e-117e-4790-a353-334baf515fd9";
        SUB_ID_4 = "329348a5-0060-4640-90b6-94e8ef048118";

        // Don't know these yet
        SUB_ID_1 = "";
        SUB_ID_2 = "";
        SUB_ID_3_9 = "";
        FEEDBACK_DISABLED = true;
    } else {
        // Hook URI for TAP Feature Announcements Test channel
        //RING_1_5_3_HOOK_URIS.Add("https://outlook.office.com/webhook/37317ed8-68c1-4564-82bb-d2acc4c6b2b4@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/b4073f0edef34b899ea708a4b6659978/512d26c9-aeed-4dbd-a16f-398bcf0ec3fe");
        // Hook URI for VanceFridge channel
        RING_1_5_3_HOOK_URIS.Add("https://outlook.office.com/webhook/15f69bfd-b260-478c-af37-db567141623a@240a3177-7696-41df-a4ea-0d1b0999fb38/IncomingWebhook/a79736f03e834b05b66a805574d20cc3/2c46ba10-5141-459c-a866-d47436de0cba");
        
        RING_4_HOOK_URIS.Add("https://outlook.office.com/webhook/15f69bfd-b260-478c-af37-db567141623a@240a3177-7696-41df-a4ea-0d1b0999fb38/IncomingWebhook/5a2da2b0ece94c75aefe55198eb432c3/2c46ba10-5141-459c-a866-d47436de0cba");

        FIELD_NAMESPACE = "Custom";
        VALIDATION_FIELD_NAMESPACE = "Custom";
        //PROJECT_NAME = "MSTeams";
        PROJECT_NAME = "MyFirstProject";
        //PASS_FAIL_URI = "https://fountainhooktesting.azurewebsites.net/api/DoNothing?code=UPhCdGVD8KLEGNZvnDzFwy1/pmZra8724PSC7zkj4oIo0pAJyRJieA==";
        PASS_FAIL_URI = "https://fountainhooktesting.azurewebsites.net/api/PassFailResponse?code=gtnp0IR3ty3Xs6QyuyqOJw//H177Uk87eWVZZikPkmelJKiabQMJ/Q==";
        RATING_URI = "https://fountainhooktesting.azurewebsites.net/api/RatingResponse?code=LsVGY5JiFDiIGMLFXOgZD4eMcAktg1Y/E3jIxwBMrEatXqPoTaJ0Kw==";
        
        SUB_ID_1_5 = "8303c1eb-d697-44cb-84ae-16d20b0ac207";
        SUB_ID_3 =   "c5b45aef-f2c7-45fb-aafb-4c6930f9e962";
        SUB_ID_4 = "329348a5-0060-4640-90b6-94e8ef048118";

        // Don't know these yet
        SUB_ID_1 = "";
        SUB_ID_2 = "";
        SUB_ID_3_9 = "";

        FEEDBACK_DISABLED = false;
    }

    log.Info("Webhook was triggered!");
    HttpContent requestContent = req.Content;
    string jsonContent = requestContent.ReadAsStringAsync().Result;

    log.Info(jsonContent);

    dynamic jsonObj = JObject.Parse(jsonContent);
    string feature = jsonObj.message.text;

    int featureId = jsonObj.resource.workItemId;
    string subscriptionId = jsonObj.subscriptionId;
    if ((subscriptionId != SUB_ID_1_5) && (subscriptionId != SUB_ID_3) && (subscriptionId != SUB_ID_4)) {
        log.Info("subscriptionId is " + subscriptionId + "; this sub not supported yet");
        return req.CreateResponse(HttpStatusCode.OK);
    }

    string featureTitle = jsonObj.resource.revision.fields["System.Title"];
    log.Info("Got title");    
    string featureDescription = "";
    try {
        featureDescription = jsonObj.resource.revision.fields["System.Description"].ToString();
        //log.Info(jsonObj.resource.revision.fields["System.Description"].ToString());
        //log.Info("Set featureDescription normally");
    } catch (Exception e) {
        featureDescription = "No description given";
        //log.Info("Set feature description to nothing given");
    }

    //log.Info("Got description");
    //log.Info(featureDescription.ToString());
    log.Info("Description length is " + featureDescription.Length);
    if (featureDescription.Length > 1500) {
        //featureDescription = Regex.Replace(featureDescription, "<.*?>", string.Empty);
        featureDescription = HtmlToText(featureDescription);
    }
    log.Info("Description length is now " + featureDescription.Length + " after converting to plaintext");

    // Truncate if it's still too long
    if (featureDescription.Length > 1500) {
        featureDescription = featureDescription.Substring(0, 1500) + "...";
    }
    log.Info("Futher truncated it");
    
    
    string featureAuthor = jsonObj.resource.revision.fields["System.ChangedBy"];

    // Feature's target date
    string featureTargetDatetime = "01/01/9999";
    string featureTargetDate = featureTargetDatetime;
    try {
        featureTargetDatetime = jsonObj.resource.revision.fields[FIELD_NAMESPACE + ".Ring4TargetDate"];
        featureTargetDate = featureTargetDatetime.Substring(0, 10);
    } catch (Exception e) { }

    // Feature's "TAP validation required?" field

    string featureValidationRequired = "Not specified";
    try {
        featureValidationRequired = jsonObj.resource.revision.fields[VALIDATION_FIELD_NAMESPACE + ".ERP_TAP_ValidationRequired"].ToString();
        if (featureValidationRequired == "")
        {
            featureValidationRequired = "Not specified";
        }
    } catch (Exception e) { }
    
    string featureUrl = jsonObj.resource._links.parent.href;

    // Fields for the internal notifications
    // Testing
    string testPlan = "";
    try {
        testPlan = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_TestPlan"];
    } catch (Exception e) { };

    string testingScenario = "";
    try {
        testingScenario = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_Testing_Scenario"];
    } catch (Exception e) { };

    string testingManualTests = "";
    try {
        testingManualTests = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_Testing_ManualTests"];
    } catch (Exception e) { };

    string testingFullTestPass = "";
    try {
        testingFullTestPass = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_Testing_FullTestPass"];
    } catch (Exception e) { };

    string testingOffshore = "";
    try {
        testingOffshore = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_Testing_Offshore"];
    } catch (Exception e) { };

    // Support and Tech Readiness
    string customerReadinessImpact = "";
    try {
        customerReadinessImpact = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-New.CustomerReadinessImpact"];
    } catch (Exception e) { };
    
    string techReadiness = "";
    try {
        techReadiness = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI.TechReadiness"];
    } catch (Exception e) { };

    string techReadinessO365 = "";
    try {
        techReadinessO365 = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_TR_O365"];
    } catch (Exception e) { };

    // TAP stuff
    // featureValidationRequired is the first TAP field
    string tapSignoff = "";
    try {
        tapSignoff = jsonObj.resource.revision.fields["MicrosoftTeamsCMMI-Copy.ERP_TAPChecklist_TAPSignoff"];
    } catch (Exception e) { };

    // Figure out which rings got enabled in this revision
    string ringValue = "";
    string availableRing = "Ring";
    int availableRingCount = 0;

    // Looking for specific ring changes in the payload.
    string[] rings = new[] {"1", "2", "1_5", "3", "3_9", "4"};

    string ringFieldName;

    foreach (string ring in rings)
    {
        log.Info("Checking out ring " + ring);
        try {
            // Somehow, the test VSTS has inconsistent namespaces. This'll have to do here
            if (!PRODUCTION)
            {
                if (ring == "1_5")
                {
                    ringFieldName = "Custom.Ring1_5";
                } else {
                    ringFieldName = "AgileRings.Ring" + ring;
                }
            }
            else {
                ringFieldName = FIELD_NAMESPACE + ".Ring" + ring;
            }

            log.Info(ringFieldName);
            log.Info(jsonObj.resource.fields[ringFieldName].ToString());
            
            ringValue = jsonObj.resource.fields[ringFieldName].newValue;
            log.Info("ringValue set");
            if ((ringValue == "Code available + Enabled") || (ringValue == "Code unavailable + Enabled")) {
                availableRing += " " + ring + " +";
                availableRingCount += 1;
            }
            
            log.Info(ringValue);

        } catch (Exception e) {}
    }

    // Make it more readable
    availableRing = availableRing.Replace("1_5", "1.5");
    availableRing = availableRing.Replace("3_9", "3.9");
    availableRing = availableRing.Trim( new Char[] { ' ', '+' } );

    if (availableRingCount > 1) {
        if (availableRing.Contains("1 ")) {
            if (subscriptionId != SUB_ID_1) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        } else if (availableRing.Contains("1.5")) {
            if (subscriptionId != SUB_ID_1_5) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        } else if (availableRing.Contains("2")) {
            if (subscriptionId != SUB_ID_2) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        } else if (availableRing.Contains("3")) {
            if (subscriptionId != SUB_ID_3) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        } else if (availableRing.Contains("3.9")) {
            if (subscriptionId != SUB_ID_3_9) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        } else if (availableRing.Contains("4")) {
            if (subscriptionId != SUB_ID_4) {
                log.Info("This is a duplicate notification. Skipping this one");
                return req.CreateResponse(HttpStatusCode.OK);
            }
        }
    }

    // Check for whether it's enabled. If not, return and do nothing
    if (availableRingCount < 1) {
        log.Info("No rings were made available in this change. Ignoring");
        return req.CreateResponse(HttpStatusCode.OK);
    }

    // Send the notification to the right destination, depending on which rings are newly enabled
    if ((availableRing.Contains("1.5") || (availableRing.Contains("3")))) {
        TEAMS_HOOK_URIS.AddRange(RING_1_5_3_HOOK_URIS);
        TEAMS_INTERNAL_HOOK_URIS.AddRange(RING_1_5_3_INTERNAL_HOOK_URIS);
    }

    if (availableRing.Contains("4")) {
        TEAMS_HOOK_URIS.AddRange(RING_4_HOOK_URIS);
        TEAMS_INTERNAL_HOOK_URIS.AddRange(RING_4_INTERNAL_HOOK_URIS);
    }

    if (availableRing.Contains("1 ")) {
        TEAMS_INTERNAL_HOOK_URIS.AddRange(RING_1_INTERNAL_HOOK_URIS);
    }

    if (availableRing.Contains("2")) {
        TEAMS_INTERNAL_HOOK_URIS.AddRange(RING_2_INTERNAL_HOOK_URIS);
    }

    if (availableRing.Contains("3.9")) {
        TEAMS_INTERNAL_HOOK_URIS.AddRange(RING_3_9_INTERNAL_HOOK_URIS);
    }

    /*
        Get the enablement status of rings 1.5, 3, and 4.
    */

    bool ring1_5, ring3, ring4, ring1, ring2, ring3_9;
    ring1_5 = false;
    ring3 = false;
    ring4 = false;

    ring1 = false;
    ring2 = false;
    ring3_9 = false;

    ringFieldName = FIELD_NAMESPACE;
    try {
        if (!PRODUCTION) {
            ringFieldName = "Custom.Ring1_5";
        } else {
            ringFieldName = FIELD_NAMESPACE + ".Ring1_5";
        }
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring1_5 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }

    try {
        if (!PRODUCTION) {
            ringFieldName = "AgileRings.Ring3";
        } else {
            ringFieldName = FIELD_NAMESPACE + ".Ring3";
        }
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring3 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }

    try {
        if (!PRODUCTION) {
            ringFieldName = "AgileRings.Ring4";
        } else {
            ringFieldName = FIELD_NAMESPACE + ".Ring4";
        }
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring4 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }

    try {
        ringFieldName = FIELD_NAMESPACE + ".Ring1";
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring1 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }

    try {
        ringFieldName = FIELD_NAMESPACE + ".Ring2";
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring2 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }

    try {
        // 3.9 is "MicrosoftTeamsCMMI-Copy.Ring3_9"
        ringFieldName = VALIDATION_FIELD_NAMESPACE + ".Ring3_9";
        string fieldValue = jsonObj.resource.revision.fields[ringFieldName];
        ring3_9 = ((fieldValue == "Code available + Enabled") || (fieldValue == "Code unavailable + Enabled"));
    } catch (Exception e) { }



    log.Info(ring1 + " " + ring1_5 + " " + ring2 +  " " + ring3 + " " + ring3_9 + " " + ring4);

    // Generate the card that will show up in Teams
    log.Info("About to make betterCard");

    dynamic betterCard = new {
        summary = "Feature now available for " + availableRing,
        title = "Feature now available for " + availableRing,
        sections = new object [] {
            new {
                activityTitle = featureTitle,
                activitySubtitle = featureDescription,
                activityImage = "https://i.imgur.com/xqG1HMv.png",
                facts = new [] {
                    new {
                        name = "Author",
                        value = featureAuthor
                    },
                    new {
                        name = "Feature expected to GA",
                        value = featureTargetDate
                    },
                    new {
                        name = "Ring 1.5",
                        value = ring1_5 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 3",
                        value = ring3 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 4",
                        value = ring4 ? "Enabled" : "Not enbaled"
                    },
                    new {
                        name = "TAP Validation Required?",
                        value = featureValidationRequired
                    }/*,
                    new {
                        name = "Pass/Fail",
                        value = "0/0"
                    }*/
                }
            },
            new {
                activityTitle = "Feedback",
                facts = new [] {
                    new {
                        name = "Pass/Fail",
                        value = "0/0"
                    },
                    new {
                        name = "Rating",
                        value = "-"
                    }
                }
            }
        },
        potentialAction = new object [] {
            new {
                inputs = new [] {
                    new {
                        type = "MultichoiceInput",
                        id = "pass-fail",
                        title = "Works as you expect?",
                        //isRequired = true,
                        style = "expanded",
                        choices = new [] {
                            new {
                                display = "Pass",
                                value = "Pass"
                            },
                            new {
                                display = "Fail",
                                value = "Fail",
                            }
                        }
                    }
                },
                type = "ActionCard",
                name = "Works as you expect?",
                actions = new [] {
                    new {
                        type = "HttpPost",
                        name = "Submit",
                        target = PASS_FAIL_URI,
                        body = new object[] {
                            new {
                                op = "add",
                                path = "/fields/System.Title",
                                from = "null",
                                value = featureTitle + " - {{pass-fail.value}}",
                                votes = new {},
                                thisCard = ""
                            },
                            new {
                                op = "add",
                                path = "/fields/System.AreaPath",
                                value = PROJECT_NAME + "\\Customer Feedback"
                            },
                            new {
                                op = "add",
                                path = "/relations/-",
                                value = new {
                                    rel = "System.LinkTypes.Related",
                                    url = featureUrl,
                                    attributes = new {
                                        isLocked = false
                                    }
                                }
                            },
                            new {
                                op = "add",
                                path = "/fields/System.Tags",
                                value = "TAP"
                            }
                        }
                    }
                }
            },
            new {
                type = "ActionCard",
                name = "Rate this feature",
                inputs = new [] {
                    new {
                        type = "MultiChoiceInput",
                        id = "rating",
                        title = "Overall rating",
                        //isRequired = true,
                        choices = new [] {
                            new {
                                display = "*****",
                                value = 5
                            },
                            new {
                                display = "****",
                                value = 4
                            },
                            new {
                                display = "***",
                                value = 3
                            },
                            new {
                                display = "**",
                                value = 2
                            },
                            new {
                                display = "*",
                                value = 1
                            },
                        },
                    }
                },
                actions = new [] {
                    new {
                        type = "HttpPOST",
                        name = "Submit",
                        target = RATING_URI,
                        body = new [] {
                            new {
                                rating = "{{rating.value}}",
                                ratings = new {},
                                thisCard = ""
                            }
                            
                        }
                    }
                }
            },
        }
    };

    dynamic internalCard = new {
        summary = "Feature now available for " + availableRing,
        title = "[Feature now available for " + availableRing + "](" + featureUrl + ")",
        sections = new object [] {
            new {
                activityTitle = "#" + featureId + ": " + featureTitle,
                activitySubtitle = featureDescription,
                activityImage = "https://i.imgur.com/xqG1HMv.png",
                facts = new [] {
                    new {
                        name = "Feature expected to GA",
                        value = featureTargetDate
                    },
                    new {
                        name = "Ring 1",
                        value = ring1 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 1.5",
                        value = ring1_5 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 2",
                        value = ring2 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 3",
                        value = ring3 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 3.9",
                        value = ring3_9 ? "Enabled" : "Not enabled"
                    },
                    new {
                        name = "Ring 4",
                        value = ring4 ? "Enabled" : "Not enbaled"
                    },
                }
            },
            new {
                activityTitle = "Testing and Automation",
                facts = new [] {
                    new {
                        name = "Before R0 - Test Plan",
                        value = testPlan
                    },
                    new {
                        name = "Before R0 - Scenario/Unit Tests",
                        value = testingScenario
                    },
                    new {
                        name = "Before R0 - Manual Tests",
                        value = testingManualTests
                    },
                    new {
                        name = "Before R0 - Manual Tests full test pass is scheduled on",
                        value = testingFullTestPass
                    },
                    new {
                        name = "Before R2 - Manual Tests - offshore vendor test pass",
                        value = testingOffshore
                    }
                }
            },
            new {
                activityTitle = "Support and Tech Readiness",
                facts = new [] {
                    new {
                        name = "Customer Readiness Impact",
                        value = customerReadinessImpact
                    },
                    new {
                        name = "TR Package",
                        value = techReadiness
                    },
                    new {
                        name = "Is O365 message center required before R4?",
                        value = techReadinessO365
                    }
                }
            },
            new {
                activityTitle = "TAP",
                facts = new [] {
                    new {
                        name = "Do you need TAP validation?",
                        value = featureValidationRequired
                    },
                    new {
                        name = "TAP Signoff",
                        value = tapSignoff
                    }
                }
            }
        }
    };

    JObject betterCardJObject = JObject.FromObject(betterCard);
    string betterCardObjectString = betterCardJObject.ToString();

    JObject internalCardJObject = JObject.FromObject(internalCard);
    

    // thisCard needs to be set to this entire card as a string.
    // This takes a few rounds, since it needs to include itself as a string as a string, etc.
    betterCardJObject["potentialAction"][0]["actions"][0]["body"][0]["thisCard"] = betterCardObjectString;
    betterCardJObject["potentialAction"][0]["actions"][0]["body"] = betterCardJObject["potentialAction"][0]["actions"][0]["body"].ToString();

    log.Info("About to access second thisCard");
    betterCardJObject["potentialAction"][1]["actions"][0]["body"][0]["thisCard"] = betterCardObjectString;
    betterCardJObject["potentialAction"][1]["actions"][0]["body"] = betterCardJObject["potentialAction"][1]["actions"][0]["body"].ToString();

    log.Info("Accessed second thisCard alright");

    // Feedback is disabled for now in production. Display the info but none of the actions.
    if (FEEDBACK_DISABLED) {
        betterCardJObject["potentialAction"] = new JArray();
        betterCardJObject["sections"][1] = new JObject();
    }

    string betterCardJson = betterCardJObject.ToString();
    // This does some double-replacement, converting type to @@type.
    betterCardJson = betterCardJson.Replace("type", "@type");
    // ...and here's how I fixed that:
    betterCardJson = betterCardJson.Replace("@@type", "@type");

    string internalCardJson = internalCardJObject.ToString();

    log.Info("betterCardJson " + betterCardJson);

    foreach (string teams_hook_uri in TEAMS_HOOK_URIS)
    {
        using (var client = new HttpClient())
        {
            var response = await client.PostAsync(
                teams_hook_uri,
                new StringContent(betterCardJson, System.Text.Encoding.UTF8, "application/json")
            );
        }
    }

    // Send internal card to internal folks
    foreach (string internal_hook_uri in TEAMS_INTERNAL_HOOK_URIS)
    {
        using (var client = new HttpClient())
        {
            var response = await client.PostAsync(
                internal_hook_uri,
                new StringContent(internalCardJson, System.Text.Encoding.UTF8, "application/json")
            );
        }
    }

    return req.CreateResponse(HttpStatusCode.OK);
}
