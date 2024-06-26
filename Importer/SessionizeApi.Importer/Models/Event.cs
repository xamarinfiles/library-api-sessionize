﻿using SessionizeApi.Importer.Dtos;
using SessionizeApi.Importer.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using XamarinFiles.FancyLogger;
using static SessionizeApi.Importer.Serialization.Configurations;
using static System.Text.Json.JsonSerializer;

namespace SessionizeApi.Importer.Models
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Event : ILogFormattable
    {
        #region Constructors

        private Event(AllDataDto allDataDto, IFancyLogger fancyLogger,
            string eventSource)
        {
            if (allDataDto == null)
                return;

            Sessions = allDataDto.Sessions
                .Select(sessionDto =>
                    Session.Create(sessionDto, fancyLogger))
                .OrderBy(session => session.Title)
                .ToArray();
            Speakers = allDataDto.Speakers
                .Select(speakerDto =>
                    Speaker.Create(speakerDto, fancyLogger))
                .OrderBy(speaker => speaker.FullName)
                .ToArray();
            Questions = allDataDto.Questions
                .Select(questionDto =>
                    Question.Create(questionDto, fancyLogger))
                .OrderBy(question => question.Sort)
                .ToArray();
            Choices = allDataDto.Choices
                .Select( choiceDto =>
                    Choice.Create(choiceDto, fancyLogger))
                .OrderBy(choice => choice.Sort)
                .ToArray();
            Rooms = allDataDto.Rooms
                .Select( roomDto =>
                    Item.Create(roomDto, fancyLogger))
                .OrderBy(room => room.Sort)
                .ToArray();

            PopulateReferenceDictionaries(fancyLogger);
            FormatReferenceFields(fancyLogger);

            FormatLogFields(eventSource, fancyLogger);
        }

        public static Event Create(string allDataJson,
            IFancyLogger fancyLogger, string eventSource = null)
        {
            try
            {
                var allData = FromJson(allDataJson, fancyLogger, eventSource);

                var @event = new Event(allData, fancyLogger, eventSource);

                return @event;
            }
            catch (Exception exception)
            {
                fancyLogger.LogException(exception);

                return null;
            }
        }

        #endregion

        // TODO Keep as arrays?
        #region Replacement API Properties

        [JsonPropertyName("sessions")]
        public Session[] Sessions { get; set; }

        [JsonPropertyName("speakers")]
        public Speaker[] Speakers { get; set; }

        [JsonPropertyName("questions")]
        public Question[] Questions { get; set; }

        [JsonPropertyName("categories")]
        public Choice[] Choices { get; set; }

        [JsonPropertyName("rooms")]
        public Item[] Rooms { get; set; }

        #endregion

        #region Reference Properties

        [JsonIgnore]
        public IDictionary<string, Item> ChoiceDictionary { get; set; }

        [JsonIgnore]
        public IDictionary<string, Session> SessionDictionary { get; set; }

        [JsonIgnore]
        public IDictionary<string, Speaker> SpeakerDictionary { get; set; }

        #endregion

        #region Formatted Properties

        [JsonIgnore]
        public string DebuggerDisplay { get; protected set; }

        [JsonIgnore]
        public string LogDisplayLong { get; protected set; }

        [JsonIgnore]
        public string LogDisplayShort { get; protected set; }

        #endregion

        #region Load Methods

        private static AllDataDto FromJson(string json,
            IFancyLogger fancyLogger, string eventSource)
        {
            try
            {
                if (Deserialize<AllDataDto>(json, DefaultReadJsonOptions) is
                    { } allData)
                {
                    return allData;
                }
            }
            catch (Exception exception)
            {
                fancyLogger.LogException(exception);
            }

            return null;
        }

        #endregion

        #region Reference Methods

        private void PopulateReferenceDictionaries(IFancyLogger fancyLogger)
        {
            try
            {
                SessionDictionary =
                    Sessions
                        .ToDictionary(session => session.Id.ToString());

                SpeakerDictionary =
                    Speakers
                        .ToDictionary(speaker => speaker.Id.ToString());

                ChoiceDictionary = new Dictionary<string, Item>();
                foreach (var (item, itemId) in
                    Choices.SelectMany(choice =>
                        choice.Items.Select(item => (item, item.Id))))
                {
                    ChoiceDictionary[itemId.ToString()] = item;
                }
            }
            catch (Exception exception)
            {
                fancyLogger.LogException(exception);
            }
        }

        private void FormatReferenceFields(IFancyLogger fancyLogger)
        {
            foreach (var session in Sessions)
            {
                session.FormatReferenceFields(SpeakerDictionary,
                    ChoiceDictionary, fancyLogger);
            }

            foreach (var speaker in Speakers)
            {
                speaker.FormatReferenceFields(SessionDictionary,
                    ChoiceDictionary, fancyLogger);
            }
        }

        #endregion

        #region Formatting Methods

        private void FormatLogFields(string eventSource,
            IFancyLogger fancyLogger)
        {
            try
            {
                DebuggerDisplay = $"Source = {eventSource ?? string.Empty}";
                LogDisplayShort =
                    DebuggerDisplay +
                    $" - |Sessions| = {Sessions.Length} - |Speakers| = {Speakers.Length}";
                // TEMP
                LogDisplayLong = LogDisplayShort;
            }
            catch (Exception exception)
            {
                fancyLogger.LogException(exception);
            }
        }

        #endregion
    }
}
