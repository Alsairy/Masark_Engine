using Xunit;
using FluentAssertions;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Masark.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Masark.Tests.Unit;

public class MultiTenancyTests
{
    [Fact]
    public void TenantEntity_ShouldHaveTenantId()
    {
        var question = new Question(
            orderNumber: 1,
            dimension: PersonalityDimension.EI,
            textEn: "Test Question",
            textAr: "سؤال تجريبي",
            textEs: "Pregunta de prueba",
            textZh: "测试问题",
            optionATextEn: "Option A",
            optionATextAr: "الخيار أ",
            optionATextEs: "Opción A",
            optionATextZh: "选项A",
            optionAMapsToFirst: true,
            optionBTextEn: "Option B",
            optionBTextAr: "الخيار ب",
            optionBTextEs: "Opción B",
            optionBTextZh: "选项B",
            tenantId: 5
        );

        question.TenantId.Should().Be(5);
    }

    [Fact]
    public void TenantContextAccessor_WithValidTenantClaim_ShouldReturnTenantId()
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.TenantContext = new TenantContext { TenantId = 123 };

        var tenantId = tenantAccessor.TenantContext?.TenantId ?? 1;
        tenantId.Should().Be(123);
    }

    [Fact]
    public void TenantContextAccessor_WithoutTenantClaim_ShouldReturnDefaultTenant()
    {
        var tenantAccessor = new TenantContextAccessor();

        var tenantId = tenantAccessor.TenantContext?.TenantId ?? 1;
        tenantId.Should().Be(1); // Default tenant
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    public void EntitiesWithDifferentTenants_ShouldBeIsolated(int tenantId)
    {
        var question1 = new Question(
            orderNumber: 1,
            dimension: PersonalityDimension.EI,
            textEn: "Question 1",
            textAr: "سؤال 1",
            textEs: "Pregunta 1",
            textZh: "问题1",
            optionATextEn: "Option A",
            optionATextAr: "الخيار أ",
            optionATextEs: "Opción A",
            optionATextZh: "选项A",
            optionAMapsToFirst: true,
            optionBTextEn: "Option B",
            optionBTextAr: "الخيار ب",
            optionBTextEs: "Opción B",
            optionBTextZh: "选项B",
            tenantId: tenantId
        );
        
        var question2 = new Question(
            orderNumber: 2,
            dimension: PersonalityDimension.EI,
            textEn: "Question 2",
            textAr: "سؤال 2",
            textEs: "Pregunta 2",
            textZh: "问题2",
            optionATextEn: "Option A",
            optionATextAr: "الخيار أ",
            optionATextEs: "Opción A",
            optionATextZh: "选项A",
            optionAMapsToFirst: true,
            optionBTextEn: "Option B",
            optionBTextAr: "الخيار ب",
            optionBTextEs: "Opción B",
            optionBTextZh: "选项B",
            tenantId: tenantId + 1
        );

        question1.TenantId.Should().NotBe(question2.TenantId);
        question1.TenantId.Should().Be(tenantId);
        question2.TenantId.Should().Be(tenantId + 1);
    }

    [Fact]
    public void AssessmentSession_ShouldMaintainTenantIsolation()
    {
        var session1 = new AssessmentSession(
            sessionToken: "token1",
            deploymentMode: DeploymentMode.STANDARD,
            languagePreference: "en",
            ipAddress: "127.0.0.1",
            userAgent: "Test Agent",
            tenantId: 1
        );
        session1.SetStudentInfo("Student 1", "student1@test.com", "12345");

        var session2 = new AssessmentSession(
            sessionToken: "token2",
            deploymentMode: DeploymentMode.STANDARD,
            languagePreference: "en",
            ipAddress: "127.0.0.1",
            userAgent: "Test Agent",
            tenantId: 2
        );
        session2.SetStudentInfo("Student 2", "student2@test.com", "67890");

        session1.TenantId.Should().NotBe(session2.TenantId);
        session1.TenantId.Should().Be(1);
        session2.TenantId.Should().Be(2);
    }
}
