using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Masark.Domain.Entities
{
    public class ReportElement : Entity, IAggregateRoot
    {
        public int? ParentElementId { get; private set; }
        public virtual ReportElement? ParentElement { get; private set; }

        public int AssessmentSessionId { get; private set; }
        public virtual AssessmentSession? AssessmentSession { get; private set; }

        public ReportElementType ElementType { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string TitleAr { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public string ContentAr { get; private set; } = string.Empty;
        public int OrderIndex { get; private set; }
        public bool IsInteractive { get; private set; }
        public string GraphData { get; private set; } = string.Empty; // JSON data for graphs
        public string ActivityData { get; private set; } = string.Empty; // JSON data for activities

        public virtual ICollection<ReportElement> ChildElements { get; private set; }
        public virtual ICollection<ReportElementQuestion> Questions { get; private set; }
        public virtual ICollection<ReportElementRating> Ratings { get; private set; }

        protected ReportElement() 
        {
            ChildElements = new List<ReportElement>();
            Questions = new List<ReportElementQuestion>();
            Ratings = new List<ReportElementRating>();
        }

        public ReportElement(int assessmentSessionId, ReportElementType elementType, string title, string titleAr,
                           string content, string contentAr, int orderIndex, bool isInteractive, int tenantId,
                           int? parentElementId = null) : base(tenantId)
        {
            AssessmentSessionId = assessmentSessionId;
            ElementType = elementType;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            TitleAr = titleAr ?? throw new ArgumentNullException(nameof(titleAr));
            Content = content ?? "";
            ContentAr = contentAr ?? "";
            OrderIndex = orderIndex;
            IsInteractive = isInteractive;
            ParentElementId = parentElementId;
            ChildElements = new List<ReportElement>();
            Questions = new List<ReportElementQuestion>();
            Ratings = new List<ReportElementRating>();
        }

        public string GetTitle(string language = "en")
        {
            return language == "ar" ? TitleAr : Title;
        }

        public string GetContent(string language = "en")
        {
            return language == "ar" ? ContentAr : Content;
        }

        public void UpdateContent(string title, string titleAr, string content, string contentAr)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            TitleAr = titleAr ?? throw new ArgumentNullException(nameof(titleAr));
            Content = content ?? "";
            ContentAr = contentAr ?? "";
            UpdateTimestamp();
        }

        public void SetGraphData(string graphData)
        {
            if (ElementType != ReportElementType.GraphSection)
                throw new InvalidOperationException("Graph data can only be set for GraphSection elements");
            
            GraphData = graphData;
            UpdateTimestamp();
        }

        public void SetActivityData(string activityData)
        {
            if (ElementType != ReportElementType.Activity)
                throw new InvalidOperationException("Activity data can only be set for Activity elements");
            
            ActivityData = activityData;
            UpdateTimestamp();
        }

        public void AddChildElement(ReportElement childElement)
        {
            if (childElement == null)
                throw new ArgumentNullException(nameof(childElement));

            childElement.ParentElementId = Id;
            ChildElements.Add(childElement);
            UpdateTimestamp();
        }

        public bool CanHaveChildren()
        {
            return ElementType == ReportElementType.Section;
        }

        public List<ReportElement> GetOrderedChildren()
        {
            return ChildElements.OrderBy(c => c.OrderIndex).ToList();
        }
    }
}
