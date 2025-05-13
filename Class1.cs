using Streamer.bot.Common.Events;
using Streamer.bot.Plugin.Interface;
using System;
using System.Collections.Generic;

public class CPHInline: CPHInlineBase
{

    readonly List<String> timeKeywords = new List<String>() 
    { 
        "hours",
        "hrs",
        "h",
        "minutes",
        "mins",
        "m",
        "seconds",
        "secs",
        "s",
    };

    /// <summary>
    ///     Streamerbot Action that can be called on command to translate a string into an amount of time 
    ///     that can be fed into AddTimeToTimer to extend the timer. 
    ///     Currently the streamer has to use the SetTime method, which is cumbersome to just add an amount of time. 
    ///     This should make it much more convenient
    ///     Supported Syntax:
    ///     !addTime 00:01:00 
    ///     !addTime X minutes
    ///     !addTime X mins
    ///     !addTime X m
    ///     !addTime X seconds
    ///     !addTime X secs
    ///     !addTime X s
    ///     !addTime X hours
    ///     !addTime X hrs
    ///     !addTime X h
    /// </summary>
    /// <returns></returns>
    public bool CommandAddTime()
    {
        int timeAdded = 0;
        string argument = "";
        if (args["rawInputEscaped"] != null)
        {
            argument = args["rawInputEscaped"].ToString();
            CPH.LogDebug($"Received argument {argument}");
            string[] tokens = argument.Split(' ');

            // It's probably a XX:XX:XX token in this case
            if (tokens.Length == 1)
            {
                CPH.LogDebug($"Only found one token {tokens[0]}, suspecting TimeString of Format hh:mm:ss");
                if (tokens[0].Split(':').Length == 3)
                {
                    // We need to remove leading zeroes, just in case
                    string hoursPosition = RemoveLeadingZero(tokens[0].Split(':')[0]);
                    string minutePosition = RemoveLeadingZero(tokens[0].Split(':')[1]);
                    string secondPosition = RemoveLeadingZero(tokens[0].Split(':')[2]);                    
                    Int32.TryParse(hoursPosition, out int hours);
                    Int32.TryParse(minutePosition, out int minutes);
                    Int32.TryParse(secondPosition, out int seconds);
                    CPH.LogDebug($"Extracted {hours} Hours, {minutes} Minutes and {seconds} seconds from argument {argument}");

                    timeAdded += (hours * 3600) + (minutes * 60) + seconds;
                    CPH.LogDebug($"Calculated additional {timeAdded} seconds from argument {argument}");
                }
            }
            //If we have more than 1 token, it's probably a long string
            else if (tokens.Length > 1)
            {
                int multiplier = 0;
                int value = 0;
                foreach (string t in tokens)
                {
                    // First we sanitize the token
                    string token = t.ToLower().Trim();

                    // If the token is in our timeKeyWords, it is a valid time unit we can use
                    // We now set the multiplayer for the actual time to add
                    if (timeKeywords.Contains(token))
                    {
                        switch (token)
                        {
                            case "hours":
                            case "hrs":
                            case "h":
                                multiplier = 3600;
                                break;
                            case "minutes":
                            case "mins":
                            case "m":
                                multiplier = 60;
                                break;
                            case "seconds":
                            case "secs":
                            case "s":
                                multiplier = 1;
                                break;
                        }
                    }
                    // If it is a parseable string, we can calulate the time added, as it should happen after we got our unit
                    else 
                    {                        
                        value = Int32.Parse(token);
                    }

                    // Just in case we check that the multiplayer is not 0
                    if(multiplier != 0 && value != 0 )
                    {
                        timeAdded += value * multiplier;
                        // Reset multiplayer now
                        multiplier = 0;
                        value = 0;
                    }
                    
                }
            }

            if (timeAdded == 0)
            {
                if (CPH.TryGetArg("msgId", out string messageId))
                {
                    CPH.LogDebug($"0 Time Added after iterating through all tokens, this should not happen");
                    CPH.TwitchReplyToMessage($"Something went wrong, this should not happen. Please contact the developer with logfiles to get this fixed.", messageId, true, true);
                }
            }
            // Add the time to the state
            AddTimeToGlobalVar(timeAdded, EventType.CommandTriggered);
            UpdateTimerLabel(
                CPH.GetGlobalVar<int>("timeInSeconds", true),
                CPH.GetGlobalVar<String>("scene", true),
                CPH.GetGlobalVar<String>("label", true),
                CPH.GetGlobalVar<String>("countdownPrefix", true));
        }
        return true;
    }

    /// <summary>
    ///     Utility Method to remove a possible leading zero from Timestrings like 01:02:01
    /// </summary>
    /// <param name="hoursPosition"></param>
    /// <returns></returns>
    private string RemoveLeadingZero(string hoursPosition)
    {
        if (hoursPosition.StartsWith("0"))
        {
            hoursPosition = hoursPosition.Remove(0, 1);
        }

        return hoursPosition;
    }

    /// <summary>
    ///     Streamerbot Action that adds Time to a timer
    ///     If secondsPerDollar is not defined, it will default to 30 seconds per Dollar
    /// </summary>
    /// <returns></returns>
    public bool AddTimeToTimer()
    {
        // Set sensible Defaults
        if(!CPH.TryGetArg<int>("secondsPerDollar", out int secondsPerDollar))
        {
            secondsPerDollar = 30;
        }
        if (!CPH.TryGetArg<int>("secondsPerHundredBits", out int secondsPerHundredBits))
        {
            secondsPerHundredBits = 30;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionPrime", out int secondsPerSubscriptionPrime))
        {
            secondsPerSubscriptionPrime = 150;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT1", out int secondsPerSubscriptionT1))
        {
            secondsPerSubscriptionT1 = 150;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT2", out int secondsPerSubscriptionT2))
        {
            secondsPerSubscriptionT2 = 250;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT3", out int secondsPerSubscriptionT3))
        {
            secondsPerSubscriptionT3 = 500;
        }

        int timeAdded = 0;
        int amountOfSubs = 1;
        
        // Find out the trigger and translate it to seconds
        EventType evt = CPH.GetEventType();
        CPH.LogDebug($"Triggered by {evt}");
        switch (evt)
        {
            case EventType.TwitchSub:
            case EventType.TwitchGiftBomb:
                if(CPH.TryGetArg<int>("gifts", out int gifts))
                {
                    amountOfSubs = gifts;
                }
                CPH.LogDebug("Triggered by evt");

                // Find out more about the sub/gift bomb
                CPH.TryGetArg<string>("tier", out string tier);
                switch(tier)
                {
                    case "prime":
                        timeAdded = secondsPerSubscriptionPrime * amountOfSubs;
                        break;
                    case "tier 1":
                        timeAdded = secondsPerSubscriptionT1 * amountOfSubs;
                        break;
                    case "tier 2":
                        timeAdded = secondsPerSubscriptionT2 * amountOfSubs;
                        break;
                    case "tier 3":
                        timeAdded = secondsPerSubscriptionT3 * amountOfSubs;
                        break;
                }
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for {amountOfSubs} * {tier} subs!");
                break;
            case EventType.TwitchCheer:
                CPH.TryGetArg<int>("bits", out int bits);
                float secondsPerBit = (float)secondsPerHundredBits / 100;
                CPH.LogDebug($"calculated {secondsPerBit} secondsPerBit");
                timeAdded = (int)Math.Floor(new decimal((float)bits * secondsPerBit));
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for Cheer of {bits} bits");
                break;
            case EventType.StreamElementsTip:
                CPH.TryGetArg<int>("tipAmount", out int donationInDollar);
                timeAdded = (donationInDollar * secondsPerDollar);
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for Tip of {donationInDollar}");
                break;
        }


        if (timeAdded > 0)
        {
            AddTimeToGlobalVar(timeAdded, evt);
        }

        UpdateTimerLabel(
            CPH.GetGlobalVar<int>("timeInSeconds", true),
            CPH.GetGlobalVar<String>("scene", true),
            CPH.GetGlobalVar<String>("label", true),
            CPH.GetGlobalVar<String>("countdownPrefix", true));

        return true;
    }

    /// <summary>
    ///     Extracted Helper-Method to handle the State of the timeAdded var
    /// </summary>
    /// <param name="timeAdded"></param>
    /// <param name="evt"></param>
    /// <returns></returns>
    private int AddTimeToGlobalVar(int timeAdded, EventType evt)
    {
        int oldTime = CPH.GetGlobalVar<int>("timeToAdd", true);
        CPH.LogDebug($"Calculated {timeAdded} additional seconds for {evt}");
        if (oldTime > 0)
        {
            CPH.LogDebug($"Adding remnant of unadded {oldTime} on top of {timeAdded}");
            timeAdded += oldTime;
        }
        CPH.LogDebug($"Setting timeToAdd to {timeAdded} now");
        CPH.SetGlobalVar("timeToAdd", timeAdded, true);
        return timeAdded;
    }

    /// <summary>
    ///     Streamerbot action that starts the timer
    /// </summary>
    /// <returns></returns>
    public bool StartTimer()
    {
        int timeInSeconds = CPH.GetGlobalVar<int>("timeInSeconds", true);
        if(timeInSeconds > 0)
        {
            ShowCountdown(timeInSeconds);
        } 
            
        return true;
    }

    /// <summary>
    ///     Private Utility-Method that handles the state and actual Time Arithmetic, as well as decreasing the time
    ///     It is a recursive method that will keep calling the Streamerbot Action "CountdownTimer", until the timer reaches 0
    ///     It relies on the args "scene" and "label" to find the correct OBS Label to set
    ///     If you want a prefix to your countdown, you can use the "countdownPrefix" Argument, which is "Countdown :" by default
    /// </summary>
    /// <param name="countdownInSeconds"></param>
    private void ShowCountdown(int countdownInSeconds)
    {
        string alertNesterScene = CPH.GetGlobalVar<String>("scene", true);
        string alertSource = CPH.GetGlobalVar<String>("label", true);
        string countdownPrefix = CPH.GetGlobalVar<String>("countdownPrefix", true);
        CPH.SetGlobalVar("timeInSeconds", countdownInSeconds, true);

        CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, true);
        if (countdownInSeconds > 0)
        {
            UpdateTimerLabel(countdownInSeconds, alertNesterScene, alertSource, countdownPrefix);
            CPH.SetGlobalVar("timeToAdd", -1, true);
            CPH.Wait(1000);

            CPH.RunAction("CountdownTimer", false);
        }
        else
        {
            CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, false);
        }
    }

    /// <summary>
    ///     Extracted Helper Method to update the Text(GDI+) Label
    /// </summary>
    /// <param name="countdownInSeconds"></param>
    /// <param name="alertNesterScene"></param>
    /// <param name="alertSource"></param>
    /// <param name="countdownPrefix"></param>
    private void UpdateTimerLabel(int countdownInSeconds, string alertNesterScene, string alertSource, string countdownPrefix)
    {
        int timeToAdd = CPH.GetGlobalVar<int>("timeToAdd", true);
        if (timeToAdd != 0)
        {
            countdownInSeconds += timeToAdd;
            CPH.LogDebug($"Adding {timeToAdd} seconds to Timer now");
            CPH.SetGlobalVar("timeToAdd", 0, true);
            CPH.SetGlobalVar("timeInSeconds", countdownInSeconds, true);
        }
        // Refetch every cycle to check for updates due to donos / commands            
        int hours = countdownInSeconds / 3600;
        int minutes = (countdownInSeconds - (hours * 3600)) / 60;
        int seconds = (countdownInSeconds - (hours * 3600)) - (minutes * 60);

        CPH.ObsSetGdiText(alertNesterScene, alertSource, $"{countdownPrefix} {hours:D2}:{minutes:D2}:{seconds:D2}");
    }

    /// <summary>
    ///     Streamerbot Action to pause the TimerQueue
    /// </summary>
    /// <returns></returns>
    public bool PauseTimer()
    {
        CPH.PauseActionQueue("CountdownTimerQueue");
        return true;
    }

    /// <summary>
    ///     Streamerbot Action to resume the TimerQueue
    /// </summary>
    /// <returns></returns>
    public bool ResumeTimer()
    {
        CPH.ResumeActionQueue("CountdownTimerQueue");
        return true;
    }
}
