﻿using SessionizeApi.Importer.Dtos;
using SessionizeApi.Importer.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using static SessionizeApi.Importer.Constants.Characters;

namespace SessionizeApi.Importer.Models
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Speaker : ILogFormattable
    {
        #region Constructor

        private Speaker(SpeakerDto oldSpeakerDto, LoggingService loggingService)
        {
            // API Properties

            Id = oldSpeakerDto.Id;
            FirstName = oldSpeakerDto.FirstName;
            LastName = oldSpeakerDto.LastName;
            Bio = oldSpeakerDto.Bio;
            TagLine = oldSpeakerDto.TagLine;
            ProfilePicture = oldSpeakerDto.ProfilePicture;
            Links = oldSpeakerDto.Links
                .Select(linkDto =>
                    Link.Create(linkDto, loggingService))
                .ToArray();
            FullName = oldSpeakerDto.FullName;
            QuestionAnswers = oldSpeakerDto.QuestionAnswers
                .Select(answerDto =>
                    QuestionAnswer.Create(answerDto,
                        loggingService))
                .ToArray();


            // Reference Properties
            SessionIds = oldSpeakerDto.SessionIds
                .Select(sessionIdUint => (Id)sessionIdUint)
                .ToArray();
            CategoryIds = oldSpeakerDto.CategoryIds
                .Select(categoryIdUint => (Id)categoryIdUint)
                .ToArray();
        }

        public static Speaker Create(SpeakerDto oldSpeakerDto,
            LoggingService loggingService)
        {
            try
            {
                var speaker = new Speaker(oldSpeakerDto, loggingService);

                return speaker;
            }
            catch (Exception exception)
            {
                loggingService.LogExceptionRouter(exception);

                return null;
            }
        }

        #endregion

        #region Original and Replacement API Properties

        [JsonPropertyName("id")]
        public Id Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("bio")]
        public string Bio { get; set; }

        [JsonPropertyName("tagLine")]
        public string TagLine { get; set; }

        [JsonPropertyName("profilePicture")]
        public Uri ProfilePicture { get; set; }

        // TODO
        //[JsonPropertyName("isTopSpeaker")]
        //public bool IsTopSpeaker { get; set; }

        [JsonPropertyName("links")]
        public Link[] Links { get; set; }

        [JsonPropertyName("sessionIds")]
        public Id[] SessionIds { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("categoryIds")]
        public Id[] CategoryIds { get; set; }

        [JsonPropertyName("questionAnswers")]
        public QuestionAnswer[] QuestionAnswers { get; set; }

        #endregion

        #region Reference Properites

        [JsonPropertyName("sessionReferences")]
        public IEnumerable<Item> SessionReferences { get; private set; }

        [JsonPropertyName("categoryReferences ")]
        public IEnumerable<Item> CategoryReferences { get; private set; }

        // TODO
        [JsonPropertyName("questionReferences")]
        public IEnumerable<Item> QuestionsReferences { get; private set; }

        #endregion

        #region Formatted Properties

        [JsonIgnore]
        public string DebuggerDisplay { get; private set; }

        [JsonIgnore]
        public string LogDisplayShort { get; private set; }

        [JsonIgnore]
        public string LogDisplayLong { get; private set; }

        #endregion

        #region Formatting Methods

        // TODO Convert dictionary arguments to refs? [Affects LINQ expression]
        // TODO Pass QuestionDictionary
        internal void FormatReferenceFields(
            IDictionary<Id, Session> sessionDictionary,
            IDictionary<Id, Item> categoryDictionary,
            LoggingService loggingService)
        {
            var sessionReferences = SessionIds
                // Dereference Session Id to get Title
                .Select(id =>
                    (id, sessionDictionary[id].Title))
                // Sort alphabetically by Session's Title
                .OrderBy(idAndName =>
                    idAndName.Title)
                // Project into Item in same alphabetical order
                .Select((idAndName, index) =>
                    Item.Create(idAndName.id, idAndName.Title, (uint)index,
                        loggingService))
                .ToList();
            SessionReferences = sessionReferences;

            var categoryReferences = CategoryIds
                // Categories are already Items => Pull directly from dictionary
                .Select(id => categoryDictionary[id])
                // Sort by Category name in case added out of alphabetical order
                .OrderBy(category => category.Name)
                .ToList();
            CategoryReferences = categoryReferences;

            // TODO Pull Question Ids and Names from dictionary => Item list

            // TODO IsTopSpeaker
            DebuggerDisplay =
                $"{FullName} - |Sessions| = {SessionIds.Length} - {Id}";

            LogDisplayShort = DebuggerDisplay;

            LogDisplayLong =
                CategoryReferences.Aggregate(LogDisplayShort,
                    (current, idAndName) =>
                        current
                        + $"{NewLine}{Indent}Category {idAndName.Id}: {idAndName.Name}");
            LogDisplayLong =
                SessionReferences.Aggregate(LogDisplayLong,
                    (current, idAndName) =>
                        current
                        + $"{NewLine}{Indent}Session {idAndName.Id}: {idAndName.Name}");
        }

        #endregion
    }
}
