using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.Finance.Ledgers;
using GameGuild.Finance.Ledgers.Abstractions;
using GameGuild.Finance.Ledgers.Commands;
using GameGuild.Finance.Ledgers.Configuration;
using GameGuild.Finance.Ledgers.Controllers;
using GameGuild.Finance.Ledgers.Entities;
using GameGuild.Finance.Ledgers.Enums;
using GameGuild.Finance.Ledgers.Handlers;
using GameGuild.Finance.Ledgers.Models;
using GameGuild.Finance.Ledgers.Queries;
using GameGuild.Finance.Ledgers.Repositories;
using GameGuild.Finance.Ledgers.Services;
using Moq;

namespace GameGuild.API.UnitTests.Finance;

public sealed class FinanceLedgersCoverageCompletionTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateOnly BusinessDate = new(2026, 5, 30);

    [Fact]
    public void LedgerDomain_CoversFactoriesMutatorsAndValidationBranches()
    {
        var root = Root("ACME ROOT", "Root Ledger");
        root.Slug.Should().Be("acme-root");
        root.HierarchyPath.Should().Be("/ACME ROOT");
        root.CanAcceptEntry().Should().BeTrue();

        var project = Ledger.CreateChild(root, LedgerType.Project, "PRJ_1", "Project", UserId, "Project ledger", "EUR");
        project.ParentLedgerId.Should().Be(root.Id);
        project.Slug.Should().Be("prj-1");
        project.CurrencyCode.Should().Be("EUR");
        root.Children.Should().Contain(project);

        var defaultCurrencyChild = Ledger.CreateChild(root, LedgerType.Department, "DEPT", "Department", UserId);
        defaultCurrencyChild.CurrencyCode.Should().Be("USD");

        var virtualLedger = Ledger.CreateVirtual(TenantId, "VIRT", "Virtual", "{}", [root.Id], UserId, "Filtered");
        virtualLedger.AllowDirectEntries.Should().BeFalse();
        virtualLedger.CanAcceptEntry().Should().BeFalse();

        Action createUnderVirtual = () => Ledger.CreateChild(virtualLedger, LedgerType.Department, "BAD", "Bad", UserId);
        createUnderVirtual.Should().Throw<InvalidOperationException>().WithMessage("*virtual*");
        Action createRootAsChild = () => Ledger.CreateChild(root, LedgerType.Root, "BAD2", "Bad2", UserId);
        createRootAsChild.Should().Throw<InvalidOperationException>().WithMessage("*root ledger as child*");

        root.Update("Renamed", "Updated", UserId);
        root.Name.Should().Be("Renamed");
        root.Description.Should().Be("Updated");
        root.UpdatedByUserId.Should().Be(UserId);
        root.UpdatedAt.Should().NotBeNull();

        foreach (var (from, to) in new[]
        {
            (LedgerStatus.Draft, LedgerStatus.Active),
            (LedgerStatus.Active, LedgerStatus.Inactive),
            (LedgerStatus.Active, LedgerStatus.Archived),
            (LedgerStatus.Active, LedgerStatus.Closed),
            (LedgerStatus.Inactive, LedgerStatus.Active),
            (LedgerStatus.Inactive, LedgerStatus.Archived),
            (LedgerStatus.Archived, LedgerStatus.Active),
        })
        {
            Set(root, nameof(Ledger.Status), from);
            root.ChangeStatus(to, UserId);
            root.Status.Should().Be(to);
        }

        Set(root, nameof(Ledger.Status), LedgerStatus.Closed);
        Action invalidTransition = () => root.ChangeStatus(LedgerStatus.Draft, UserId);
        invalidTransition.Should().Throw<InvalidOperationException>().WithMessage("*Cannot transition*");

        root.SetBudgetLimit(123.45m, UserId);
        root.BudgetLimit.Should().Be(123.45m);

        Action nonProjectDates = () => root.SetProjectDates(BusinessDate, BusinessDate.AddDays(1), UserId);
        nonProjectDates.Should().Throw<InvalidOperationException>().WithMessage("*Project dates*");
        Action invalidDates = () => project.SetProjectDates(BusinessDate.AddDays(1), BusinessDate, UserId);
        invalidDates.Should().Throw<ArgumentException>().WithMessage("*before end date*");
        project.SetProjectDates(BusinessDate, BusinessDate.AddDays(10), UserId);
        project.ProjectStartDate.Should().Be(BusinessDate);
        project.ProjectEndDate.Should().Be(BusinessDate.AddDays(10));
        project.SetProjectDates(BusinessDate, BusinessDate, UserId);
        project.ProjectEndDate.Should().Be(BusinessDate);
        project.SetProjectDates(null, BusinessDate.AddDays(20), UserId);
        project.ProjectStartDate.Should().BeNull();
        project.SetProjectDates(BusinessDate, null, UserId);
        project.ProjectEndDate.Should().BeNull();
        project.SetProjectDates(null, null, UserId);
        project.ProjectStartDate.Should().BeNull();
        project.ProjectEndDate.Should().BeNull();

        root.UpdateCachedStats(50m, 20m, 3);
        root.CachedTotalDebit.Should().Be(50m);
        root.CachedTotalCredit.Should().Be(20m);
        root.CachedNetBalance.Should().Be(30m);
        root.CachedEntryCount.Should().Be(3);

        var parentWithChild = Root("PARENT");
        Ledger.CreateChild(parentWithChild, LedgerType.Department, "CHILD", "Child", UserId);
        Action deleteWithChild = () => parentWithChild.SoftDelete(UserId);
        deleteWithChild.Should().Throw<InvalidOperationException>().WithMessage("*active children*");

        var deletable = Root("DELETE");
        deletable.SoftDelete(UserId);
        deletable.DeletedAt.Should().NotBeNull();
        deletable.CanAcceptEntry().Should().BeFalse();

        var inactive = Root("INACTIVE");
        Set(inactive, nameof(Ledger.Status), LedgerStatus.Inactive);
        inactive.CanAcceptEntry().Should().BeFalse();
        var noDirectEntries = Root("NODIRECT");
        Set(noDirectEntries, nameof(Ledger.AllowDirectEntries), false);
        noDirectEntries.CanAcceptEntry().Should().BeFalse();
        Set(virtualLedger, nameof(Ledger.AllowDirectEntries), true);
        virtualLedger.CanAcceptEntry().Should().BeFalse();
    }

    [Fact]
    public void LedgerEntryDomain_CoversLifecycleTransferReversalAndDeletionBranches()
    {
        var ledger = Root("ENTRY");
        var ancestorId = Guid.NewGuid();

        var entry = LedgerEntry.Create(
            ledger,
            [ancestorId],
            EntryType.Debit,
            EntryCategory.Expense,
            42m,
            BusinessDate,
            "Expense",
            UserId,
            "ext-1");

        entry.ReferenceNumber.Should().StartWith("TXN-");
        entry.ParentLedgerIds.Should().Contain(ancestorId);
        entry.Status.Should().Be(EntryStatus.Pending);
        entry.GetSignedAmount().Should().Be(42m);

        Action zeroAmount = () => LedgerEntry.Create(ledger, [], EntryType.Debit, EntryCategory.Expense, 0m, BusinessDate, "bad", UserId);
        zeroAmount.Should().Throw<ArgumentException>().WithParameterName("amount");

        var inactiveLedger = Root("ENTRY-INACTIVE");
        Set(inactiveLedger, nameof(Ledger.Status), LedgerStatus.Inactive);
        Action invalidLedger = () => LedgerEntry.Create(inactiveLedger, [], EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "bad", UserId);
        invalidLedger.Should().Throw<InvalidOperationException>().WithMessage("*cannot accept entries*");

        entry.Post(UserId);
        entry.Status.Should().Be(EntryStatus.Posted);
        Action repost = () => entry.Post(UserId);
        repost.Should().Throw<InvalidOperationException>().WithMessage("*Posted status*");

        Action updatePosted = () => entry.UpdateDescription("Updated", "Notes", UserId);
        updatePosted.Should().NotThrow();
        entry.Notes.Should().Be("Notes");

        entry.Reconcile(UserId, "bank-1");
        entry.Status.Should().Be(EntryStatus.Reconciled);
        entry.ExternalReferenceId.Should().Be("bank-1");
        Action reconcileAgain = () => entry.Reconcile(UserId);
        reconcileAgain.Should().Throw<InvalidOperationException>().WithMessage("*Only posted entries*");
        Action updateReconciled = () => entry.UpdateDescription("No", null, UserId);
        updateReconciled.Should().Throw<InvalidOperationException>().WithMessage("*reconciled*");
        Action deleteReconciled = () => entry.EnsureCanDelete();
        deleteReconciled.Should().Throw<InvalidOperationException>().WithMessage("*reconciled*");

        var original = PostedEntry(ledger, EntryType.Credit, EntryCategory.Revenue, 77m);
        var reversal = original.CreateReversal(UserId, "correction");
        original.Status.Should().Be(EntryStatus.Voided);
        reversal.Type.Should().Be(EntryType.Debit);
        reversal.ReversesEntryId.Should().Be(original.Id);
        Action deleteReversal = () => reversal.EnsureCanDelete();
        deleteReversal.Should().Throw<InvalidOperationException>().WithMessage("*reversal chain*");
        Action reverseVoided = () => original.CreateReversal(UserId, "again");
        reverseVoided.Should().Throw<InvalidOperationException>().WithMessage("*already voided*");
        Action deleteVoided = () => original.EnsureCanDelete();
        deleteVoided.Should().Throw<InvalidOperationException>().WithMessage("*voided*");

        var reversedBy = PostedEntry(ledger);
        Set(reversedBy, nameof(LedgerEntry.ReversedByEntryId), Guid.NewGuid());
        Action reverseTwice = () => reversedBy.CreateReversal(UserId, "again");
        reverseTwice.Should().Throw<InvalidOperationException>().WithMessage("*already been reversed*");
        Action deleteReversalChain = () => reversedBy.EnsureCanDelete();
        deleteReversalChain.Should().Throw<InvalidOperationException>().WithMessage("*reversal chain*");

        var transferSourceLedger = Root("SRC");
        var transferDestinationLedger = Root("DST");
        var (source, destination) = LedgerEntry.CreateTransfer(
            transferSourceLedger,
            [transferSourceLedger.Id],
            transferDestinationLedger,
            [transferDestinationLedger.Id],
            15m,
            BusinessDate,
            "Move cash",
            UserId);
        source.Type.Should().Be(EntryType.Credit);
        destination.Type.Should().Be(EntryType.Debit);
        source.TransferPairEntryId.Should().Be(destination.Id);
        destination.TransferPairEntryId.Should().Be(source.Id);
        Action deleteTransfer = () => source.EnsureCanDelete();
        deleteTransfer.Should().Throw<InvalidOperationException>().WithMessage("*transfer entries*");

        var mutable = LedgerEntry.Create(ledger, [], EntryType.Debit, EntryCategory.Asset, 5m, BusinessDate, "Asset", UserId);
        mutable.SetCurrencyConversion(10m, "BRL", 0.5m);
        mutable.AssociateBudget(Guid.NewGuid(), "OPS", "2026-05");
        mutable.UpdateDescription("Updated asset", null, UserId);
        mutable.OriginalAmount.Should().Be(10m);
        mutable.BudgetCategoryCode.Should().Be("OPS");
        mutable.EnsureCanDelete();

        Set(mutable, nameof(LedgerEntry.Status), EntryStatus.Voided);
        Action updateVoided = () => mutable.UpdateDescription("No", null, UserId);
        updateVoided.Should().Throw<InvalidOperationException>().WithMessage("*voided*");

        LedgerEntry.Create(ledger, [], EntryType.Credit, EntryCategory.Revenue, 9m, BusinessDate, "Revenue", UserId)
            .GetSignedAmount()
            .Should()
            .Be(-9m);
    }

    [Fact]
    public void LedgerClosureFactories_CoverSelfChildAndTenantValidationBranches()
    {
        var root = Root("CLOSURE");
        var child = Ledger.CreateChild(root, LedgerType.Department, "CLOSURE-CHILD", "Child", UserId);

        var self = LedgerClosure.CreateSelfReference(root);
        self.AncestorId.Should().Be(root.Id);
        self.DescendantId.Should().Be(root.Id);
        self.Depth.Should().Be(0);

        var childClosures = LedgerClosure.CreateForNewChild(child, [self]).ToList();
        childClosures.Should().HaveCount(2);
        childClosures[0].Depth.Should().Be(0);
        childClosures[1].AncestorId.Should().Be(root.Id);
        childClosures[1].Depth.Should().Be(1);

        var direct = LedgerClosure.Create(root, child, 1);
        direct.TenantId.Should().Be(TenantId);

        var otherTenant = Ledger.CreateRoot(Guid.NewGuid(), "OTHER", "Other", "USD", UserId);
        Action crossTenant = () => LedgerClosure.Create(root, otherTenant, 1);
        crossTenant.Should().Throw<InvalidOperationException>().WithMessage("*same tenant*");
    }

    [Fact]
    public async Task LedgerHierarchyService_CoversSuccessAndFailureBranches()
    {
        var ledgers = new FakeLedgerRepository();
        var closures = new FakeLedgerClosureRepository();
        var service = new LedgerHierarchyService(ledgers, closures);

        var root = await service.CreateRootLedgerAsync(TenantId, "ROOT", "Root", "USD", UserId, "desc");
        ledgers.AddedLedgers.Should().Contain(root);
        closures.AddedClosures.Should().ContainSingle(c => c.AncestorId == root.Id && c.DescendantId == root.Id);

        Func<Task> duplicateRoot = () => service.CreateRootLedgerAsync(TenantId, "ROOT", "Root", "USD", UserId);
        await duplicateRoot.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");

        var child = await service.CreateChildLedgerAsync(root.Id, LedgerType.Department, "DEPT", "Dept", UserId);
        child.ParentLedgerId.Should().Be(root.Id);
        closures.AddedClosures.Should().Contain(c => c.DescendantId == child.Id);

        Func<Task> missingParent = () => service.CreateChildLedgerAsync(Guid.NewGuid(), LedgerType.Department, "MISS", "Missing", UserId);
        await missingParent.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Parent ledger*");

        Func<Task> duplicateChild = () => service.CreateChildLedgerAsync(root.Id, LedgerType.Department, "DEPT", "Duplicate", UserId);
        await duplicateChild.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");

        var tree = await service.GetHierarchyTreeAsync(root.Id, maxDepth: 2);
        tree.Code.Should().Be("ROOT");
        tree.Children.Should().ContainSingle(c => c.Code == "DEPT");

        var shallowTree = await service.GetHierarchyTreeAsync(root.Id, maxDepth: 0);
        shallowTree.Children.Should().BeEmpty();

        Func<Task> missingTree = () => service.GetHierarchyTreeAsync(Guid.NewGuid());
        await missingTree.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        closures.AncestorsByDescendant[child.Id] =
        [
            LedgerClosure.Create(root, child, 1),
            LedgerClosure.Create(child, child, 0)
        ];
        var path = await service.GetAncestorPathAsync(child.Id);
        path.Select(l => l.Code).Should().Equal("ROOT", "DEPT");

        (await service.CanMoveToParentAsync(child.Id, child.Id)).Should().BeFalse();
        closures.IsAncestorOverride = true;
        (await service.CanMoveToParentAsync(root.Id, child.Id)).Should().BeFalse();
        closures.IsAncestorOverride = false;
        (await service.CanMoveToParentAsync(Guid.NewGuid(), child.Id)).Should().BeFalse();

        var otherTenantRoot = Ledger.CreateRoot(Guid.NewGuid(), "OTHER-TENANT", "Other", "USD", UserId);
        ledgers.Seed(otherTenantRoot);
        (await service.CanMoveToParentAsync(child.Id, otherTenantRoot.Id)).Should().BeFalse();
        (await service.CanMoveToParentAsync(root.Id, child.Id)).Should().BeFalse();

        var virtualParent = Ledger.CreateVirtual(TenantId, "VIRTUAL-PARENT", "Virtual", "{}", [root.Id], UserId);
        ledgers.Seed(virtualParent);
        (await service.CanMoveToParentAsync(child.Id, virtualParent.Id)).Should().BeFalse();
        (await service.CanMoveToParentAsync(child.Id, root.Id)).Should().BeTrue();

        Func<Task> invalidMove = () => service.MoveLedgerAsync(root.Id, child.Id, UserId);
        await invalidMove.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot move*");

        await service.MoveLedgerAsync(child.Id, root.Id, UserId);
        closures.DeletedDescendants.Should().Contain(child.Id);
        ledgers.UpdatedLedgers.Should().Contain(child);
    }

    [Fact]
    public async Task LedgerEntryService_CoversPostingTransferBatchReverseReconcileAndDelete()
    {
        var source = Root("SOURCE");
        var destination = Root("DESTINATION");
        var inactive = Root("NOPOST");
        Set(inactive, nameof(Ledger.Status), LedgerStatus.Inactive);

        var ledgers = new FakeLedgerRepository(source, destination, inactive);
        var closures = new FakeLedgerClosureRepository();
        closures.AncestorIdsByDescendant[source.Id] = [Guid.NewGuid(), source.Id];
        closures.AncestorIdsByDescendant[destination.Id] = [Guid.NewGuid(), destination.Id];
        var entries = new FakeLedgerEntryRepository();
        var service = new LedgerEntryService(ledgers, closures, entries, NullLogger<LedgerEntryService>.Instance);

        var posted = await service.PostEntryAsync(
            source.Id,
            EntryType.Debit,
            EntryCategory.Expense,
            10m,
            BusinessDate,
            "Expense",
            UserId,
            "external");
        posted.Status.Should().Be(EntryStatus.Posted);
        entries.Entries.Should().Contain(posted);

        Func<Task> missingLedgerPost = () => service.PostEntryAsync(Guid.NewGuid(), EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "bad", UserId);
        await missingLedgerPost.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> inactivePost = () => service.PostEntryAsync(inactive.Id, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "bad", UserId);
        await inactivePost.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot accept entries*");

        var transfer = await service.PostTransferAsync(source.Id, destination.Id, 25m, BusinessDate, "Transfer", UserId);
        transfer.Source.Type.Should().Be(EntryType.Credit);
        transfer.Destination.Type.Should().Be(EntryType.Debit);
        entries.Entries.Should().Contain([transfer.Source, transfer.Destination]);

        Func<Task> missingSource = () => service.PostTransferAsync(Guid.NewGuid(), destination.Id, 1m, BusinessDate, "bad", UserId);
        await missingSource.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Source ledger*");
        Func<Task> missingDestination = () => service.PostTransferAsync(source.Id, Guid.NewGuid(), 1m, BusinessDate, "bad", UserId);
        await missingDestination.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Destination ledger*");
        Func<Task> inactiveSource = () => service.PostTransferAsync(inactive.Id, destination.Id, 1m, BusinessDate, "bad", UserId);
        await inactiveSource.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Source ledger*");
        Func<Task> inactiveDestination = () => service.PostTransferAsync(source.Id, inactive.Id, 1m, BusinessDate, "bad", UserId);
        await inactiveDestination.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Destination ledger*");

        var update = await service.UpdateEntryAsync(posted.Id, "Updated", "note", UserId);
        update.Description.Should().Be("Updated");
        Func<Task> missingUpdate = () => service.UpdateEntryAsync(Guid.NewGuid(), "missing", null, UserId);
        await missingUpdate.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        var batch = await service.PostBatchAsync(
            [
                new CreateEntryRequest(source.Id, EntryType.Debit, EntryCategory.Expense, 11m, BusinessDate, "One", BudgetId: Guid.NewGuid(), BudgetCategoryCode: "OPS"),
                new CreateEntryRequest(destination.Id, EntryType.Credit, EntryCategory.Revenue, 12m, BusinessDate, "Two")
            ],
            UserId);
        batch.Should().HaveCount(2);
        batch[0].BudgetCategoryCode.Should().Be("OPS");

        Func<Task> batchMissingLedger = () => service.PostBatchAsync([new CreateEntryRequest(Guid.NewGuid(), EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "bad")], UserId);
        await batchMissingLedger.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> batchInactiveLedger = () => service.PostBatchAsync([new CreateEntryRequest(inactive.Id, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "bad")], UserId);
        await batchInactiveLedger.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot accept entries*");

        await service.ReconcileEntryAsync(posted.Id, UserId, "bank");
        posted.Status.Should().Be(EntryStatus.Reconciled);
        Func<Task> missingReconcile = () => service.ReconcileEntryAsync(Guid.NewGuid(), UserId);
        await missingReconcile.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        var reversible = PostedEntry(source);
        entries.Seed(reversible);
        var reversal = await service.ReverseEntryAsync(reversible.Id, "void", UserId);
        reversal.ReversesEntryId.Should().Be(reversible.Id);
        entries.Entries.Should().Contain(reversal);

        var pairSource = PostedEntry(source);
        var pairDestination = PostedEntry(destination);
        Set(pairSource, nameof(LedgerEntry.TransferPairEntryId), pairDestination.Id);
        entries.Seed(pairSource, pairDestination);
        var pairReversal = await service.ReverseEntryAsync(pairSource.Id, "void transfer", UserId);
        pairReversal.ReversesEntryId.Should().Be(pairSource.Id);
        pairDestination.Status.Should().Be(EntryStatus.Voided);

        var voidedPair = PostedEntry(destination);
        voidedPair.CreateReversal(UserId, "already");
        var sourceWithVoidedPair = PostedEntry(source);
        Set(sourceWithVoidedPair, nameof(LedgerEntry.TransferPairEntryId), voidedPair.Id);
        entries.Seed(sourceWithVoidedPair, voidedPair);
        await service.ReverseEntryAsync(sourceWithVoidedPair.Id, "source only", UserId);

        Func<Task> missingReverse = () => service.ReverseEntryAsync(Guid.NewGuid(), "missing", UserId);
        await missingReverse.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        var deletable = LedgerEntry.Create(source, [], EntryType.Debit, EntryCategory.Asset, 3m, BusinessDate, "delete", UserId);
        entries.Seed(deletable);
        await service.DeleteEntryAsync(deletable.Id, UserId);
        entries.Entries.Should().NotContain(deletable);
        Func<Task> missingDelete = () => service.DeleteEntryAsync(Guid.NewGuid(), UserId);
        await missingDelete.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task LedgerRollupService_CoversBalancesRollupsPeriodsAndRefreshBranches()
    {
        var root = Root("ROLLUP");
        var child = Ledger.CreateChild(root, LedgerType.Department, "ROLLUP-CHILD", "Child", UserId);
        var ledgers = new FakeLedgerRepository(root, child);
        var closures = new FakeLedgerClosureRepository();
        closures.DescendantIdsByAncestor[root.Id] = [root.Id, child.Id];
        var entries = new FakeLedgerEntryRepository();
        entries.Seed(
            PostedEntry(root, EntryType.Debit, EntryCategory.Expense, 10m, new DateOnly(2026, 1, 15)),
            PostedEntry(root, EntryType.Credit, EntryCategory.Revenue, 40m, new DateOnly(2026, 2, 1)),
            PostedEntry(child, EntryType.Debit, EntryCategory.Asset, 5m, new DateOnly(2026, 3, 2), [root.Id]));

        var voided = PostedEntry(root, EntryType.Credit, EntryCategory.Revenue, 100m, new DateOnly(2026, 4, 1));
        voided.CreateReversal(UserId, "ignore");
        entries.Seed(voided);

        var service = new LedgerRollupService(ledgers, closures, entries);

        var balance = await service.GetBalanceAsync(root.Id);
        balance.TotalDebit.Should().Be(10m);
        balance.TotalCredit.Should().Be(40m);
        balance.NetBalance.Should().Be(30m);
        balance.IncludesDescendants.Should().BeFalse();

        var datedBalance = await service.GetBalanceAsync(root.Id, new DateOnly(2026, 1, 31));
        datedBalance.TotalDebit.Should().Be(10m);
        datedBalance.TotalCredit.Should().Be(0m);

        var consolidated = await service.GetConsolidatedBalanceAsync(root.Id, new DateOnly(2026, 12, 31));
        consolidated.TotalDebit.Should().Be(15m);
        consolidated.TotalCredit.Should().Be(40m);
        consolidated.IncludesDescendants.Should().BeTrue();

        Func<Task> missingBalance = () => service.GetBalanceAsync(Guid.NewGuid());
        await missingBalance.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> missingConsolidated = () => service.GetConsolidatedBalanceAsync(Guid.NewGuid());
        await missingConsolidated.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        var hierarchy = await service.GetHierarchicalRollupAsync(root.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        hierarchy.Children.Should().ContainSingle();
        hierarchy.ConsolidatedBalance.TotalDebit.Should().Be(15m);
        Func<Task> missingHierarchy = () => service.GetHierarchicalRollupAsync(Guid.NewGuid());
        await missingHierarchy.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        var categories = await service.GetCategoryRollupAsync(root.Id, includeDescendants: true);
        categories.Should().Contain(c => c.Category == EntryCategory.Revenue && c.PercentageOfTotal > 0m);

        var emptyCategories = await service.GetCategoryRollupAsync(Guid.NewGuid(), includeDescendants: false);
        emptyCategories.Should().BeEmpty();

        var zeroAmount = PostedEntry(root, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate);
        Set(zeroAmount, nameof(LedgerEntry.Amount), 0m);
        entries.Seed(zeroAmount);
        var zeroRollup = await service.GetCategoryRollupAsync(root.Id, includeDescendants: false, BusinessDate, BusinessDate);
        zeroRollup.Should().Contain(c => c.PercentageOfTotal == 0m);

        foreach (var granularity in Enum.GetValues<PeriodGranularity>())
        {
            var periods = await service.GetPeriodRollupAsync(
                root.Id,
                includeDescendants: granularity == PeriodGranularity.Week,
                new DateOnly(2026, 1, 15),
                new DateOnly(2026, 12, 31),
                granularity);
            periods.Should().NotBeEmpty();
            periods.Select(p => p.PeriodLabel).Should().OnlyContain(label => !string.IsNullOrWhiteSpace(label));
        }

        var defaultGranularityPeriods = await service.GetPeriodRollupAsync(
            root.Id,
            includeDescendants: false,
            BusinessDate,
            BusinessDate,
            (PeriodGranularity)999);
        defaultGranularityPeriods.Should().ContainSingle();

        Func<Task> invalidPeriod = () => service.GetPeriodRollupAsync(root.Id, false, BusinessDate, BusinessDate.AddDays(-1), PeriodGranularity.Month);
        await invalidPeriod.Should().ThrowAsync<ArgumentException>().WithParameterName("toDate");

        await service.RefreshLedgerStatsAsync(root.Id);
        root.CachedEntryCount.Should().BeGreaterThan(0);
        ledgers.UpdatedLedgers.Should().Contain(root);

        Func<Task> missingRefresh = () => service.RefreshLedgerStatsAsync(Guid.NewGuid());
        await missingRefresh.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");

        await service.RefreshSubtreeStatsAsync(root.Id);
        ledgers.UpdatedLedgers.Should().Contain(child);
    }

    [Fact]
    public async Task VirtualLedgerService_CoversCreationFilterParsingFilteringBalanceAndFailures()
    {
        var sourceOne = Root("SRC1");
        var sourceTwo = Root("SRC2");
        var nonVirtual = Root("REAL");
        var ledgers = new FakeLedgerRepository(sourceOne, sourceTwo, nonVirtual);
        var entries = new FakeLedgerEntryRepository();
        var closures = new FakeLedgerClosureRepository();

        var expense = PostedEntry(sourceOne, EntryType.Debit, EntryCategory.Expense, 10m, new DateOnly(2026, 5, 1));
        expense.Tags.Add("ops");
        var revenue = PostedEntry(sourceTwo, EntryType.Credit, EntryCategory.Revenue, 20m, new DateOnly(2026, 5, 2), [sourceOne.Id]);
        revenue.Tags.Add("sales");
        var tooLarge = PostedEntry(sourceOne, EntryType.Debit, EntryCategory.Expense, 99m, new DateOnly(2026, 5, 3));
        tooLarge.Tags.Add("ops");
        entries.Seed(expense, revenue, tooLarge);

        var service = new VirtualLedgerService(ledgers, entries, closures);
        var filter = new VirtualLedgerFilter(
            SourceLedgerIds: [sourceOne.Id, sourceTwo.Id],
            Categories: [EntryCategory.Expense],
            Tags: ["ops"],
            MinAmount: 5m,
            MaxAmount: 50m,
            IncludeDescendants: false);

        var virtualLedger = await service.CreateVirtualLedgerAsync(TenantId, "VIRTUAL", "Virtual", filter, UserId, "desc");
        virtualLedger.Type.Should().Be(LedgerType.Virtual);

        Func<Task> duplicateVirtual = () => service.CreateVirtualLedgerAsync(TenantId, "VIRTUAL", "Virtual", filter, UserId);
        await duplicateVirtual.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");

        await service.UpdateFilterAsync(virtualLedger.Id, filter, UserId);
        ledgers.UpdatedLedgers.Should().Contain(virtualLedger);
        Func<Task> missingUpdate = () => service.UpdateFilterAsync(Guid.NewGuid(), filter, UserId);
        await missingUpdate.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> nonVirtualUpdate = () => service.UpdateFilterAsync(nonVirtual.Id, filter, UserId);
        await nonVirtualUpdate.Should().ThrowAsync<InvalidOperationException>().WithMessage("*virtual ledgers*");

        var filtered = await service.GetVirtualEntriesAsync(virtualLedger.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), skip: 0, take: 10);
        filtered.Should().ContainSingle().Which.Id.Should().Be(expense.Id);

        var pagedOut = await service.GetVirtualEntriesAsync(virtualLedger.Id, skip: 1, take: 1);
        pagedOut.Should().BeEmpty();

        var descendantFilter = new VirtualLedgerFilter(SourceLedgerIds: [sourceOne.Id], IncludeDescendants: true);
        var descendantVirtual = await service.CreateVirtualLedgerAsync(TenantId, "VIRTUAL-DESC", "Virtual Desc", descendantFilter, UserId);
        var descendantEntries = await service.GetVirtualEntriesAsync(descendantVirtual.Id);
        descendantEntries.Should().Contain(e => e.Id == revenue.Id);

        var ledgerSourceFallback = Ledger.CreateVirtual(TenantId, "FALLBACK", "Fallback", "", [sourceOne.Id], UserId);
        ledgers.Seed(ledgerSourceFallback);
        (await service.GetVirtualEntriesAsync(ledgerSourceFallback.Id)).Should().NotBeEmpty();

        var invalidJsonLedger = Ledger.CreateVirtual(TenantId, "BADJSON", "BadJson", "{", [sourceOne.Id], UserId);
        ledgers.Seed(invalidJsonLedger);
        (await service.GetVirtualEntriesAsync(invalidJsonLedger.Id)).Should().NotBeEmpty();

        var nullJsonLedger = Ledger.CreateVirtual(TenantId, "NULLJSON", "NullJson", "null", [sourceOne.Id], UserId);
        ledgers.Seed(nullJsonLedger);
        (await service.GetVirtualEntriesAsync(nullJsonLedger.Id)).Should().NotBeEmpty();

        var balance = await service.GetVirtualBalanceAsync(virtualLedger.Id, new DateOnly(2026, 5, 31));
        balance.TotalDebit.Should().Be(10m);
        balance.TotalCredit.Should().Be(0m);
        balance.IncludesDescendants.Should().BeTrue();

        Func<Task> missingEntries = () => service.GetVirtualEntriesAsync(Guid.NewGuid());
        await missingEntries.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> nonVirtualEntries = () => service.GetVirtualEntriesAsync(nonVirtual.Id);
        await nonVirtualEntries.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Not a virtual ledger*");
        Func<Task> missingBalance = () => service.GetVirtualBalanceAsync(Guid.NewGuid());
        await missingBalance.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        Func<Task> nonVirtualBalance = () => service.GetVirtualBalanceAsync(nonVirtual.Id);
        await nonVirtualBalance.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Not a virtual ledger*");
    }

    [Fact]
    public async Task Handlers_CoverLedgerEntryAndBalanceCQRSPaths()
    {
        var ledger = Root("HANDLER");
        var entry = PostedEntry(ledger);
        var ledgerRepository = new Mock<ILedgerRepository>();
        var closureRepository = new Mock<ILedgerClosureRepository>();
        var hierarchyService = new Mock<ILedgerHierarchyService>();
        var entryService = new Mock<ILedgerEntryService>();
        var rollupService = new Mock<ILedgerRollupService>();
        var virtualService = new Mock<IVirtualLedgerService>();
        var entryRepository = new Mock<ILedgerEntryRepository>();

        hierarchyService.Setup(s => s.CreateRootLedgerAsync(TenantId, "ROOT", "Root", "USD", UserId, "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);
        hierarchyService.Setup(s => s.CreateChildLedgerAsync(ledger.Id, LedgerType.Department, "CHILD", "Child", UserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ledger.CreateChild(ledger, LedgerType.Department, "CHILD", "Child", UserId));
        hierarchyService.Setup(s => s.GetHierarchyTreeAsync(ledger.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LedgerTreeNode { Id = ledger.Id, Code = ledger.Code, Name = ledger.Name, Type = ledger.Type, Status = ledger.Status, CurrencyCode = ledger.CurrencyCode, Depth = 0 });
        hierarchyService.Setup(s => s.GetAncestorPathAsync(ledger.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ledger]);

        ledgerRepository.Setup(r => r.GetByIdAsync(ledger.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
        ledgerRepository.Setup(r => r.GetByCodeAsync(TenantId, ledger.Code, It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
        ledgerRepository.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        ledgerRepository.Setup(r => r.GetRootLedgersAsync(TenantId, It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        ledgerRepository.Setup(r => r.GetChildrenAsync(ledger.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        ledgerRepository.Setup(r => r.GetVirtualLedgersAsync(TenantId, It.IsAny<CancellationToken>())).ReturnsAsync([Ledger.CreateVirtual(TenantId, "V", "Virtual", "{}", [ledger.Id], UserId)]);
        closureRepository.Setup(r => r.GetDescendantsAsync(ledger.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([LedgerClosure.Create(ledger, ledger, 0)]);

        entryService.Setup(s => s.PostEntryAsync(ledger.Id, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", UserId, "ext", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        entryService.Setup(s => s.PostTransferAsync(ledger.Id, ledger.Id, 1m, BusinessDate, "Transfer", UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entry, PostedEntry(ledger, EntryType.Debit)));
        entryService.Setup(s => s.ReverseEntryAsync(entry.Id, "reason", UserId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        entryService.Setup(s => s.UpdateEntryAsync(entry.Id, "updated", "notes", UserId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        entryService.Setup(s => s.PostBatchAsync(It.IsAny<IEnumerable<CreateEntryRequest>>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([entry]);
        entryRepository.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        var nullLedgerEntry = PostedEntry(ledger);
        Set<Ledger?>(nullLedgerEntry, nameof(LedgerEntry.Ledger), null);
        entryRepository.Setup(r => r.GetByIdAsync(nullLedgerEntry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nullLedgerEntry);
        entryRepository.Setup(r => r.GetByLedgerAsync(ledger.Id, null, null, It.IsAny<CancellationToken>())).ReturnsAsync([entry]);
        entryRepository.Setup(r => r.GetByLedgerIncludingDescendantsAsync(ledger.Id, null, null, It.IsAny<CancellationToken>())).ReturnsAsync([entry]);
        virtualService.Setup(s => s.GetVirtualEntriesAsync(ledger.Id, null, null, 0, 50, It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        var balance = new LedgerBalance(ledger.Id, ledger.Code, ledger.Name, 1m, 2m, 1m, 2, "USD", DateTime.UtcNow);
        var rollup = new LedgerRollupNode { Id = ledger.Id, Code = ledger.Code, Name = ledger.Name, Type = ledger.Type, Depth = 0, OwnBalance = balance, ConsolidatedBalance = balance };
        rollupService.Setup(s => s.GetBalanceAsync(ledger.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(balance);
        rollupService.Setup(s => s.GetConsolidatedBalanceAsync(ledger.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(balance);
        rollupService.Setup(s => s.GetHierarchicalRollupAsync(ledger.Id, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(rollup);
        rollupService.Setup(s => s.GetCategoryRollupAsync(ledger.Id, false, null, null, It.IsAny<CancellationToken>())).ReturnsAsync([new CategoryRollup(EntryCategory.Expense, 1m, 0m, -1m, 1, 100m)]);
        rollupService.Setup(s => s.GetPeriodRollupAsync(ledger.Id, false, BusinessDate, BusinessDate, PeriodGranularity.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PeriodRollup(BusinessDate, BusinessDate, "May 2026", 1m, 0m, -1m, -1m, 1)]);

        (await new CreateRootLedgerHandler(hierarchyService.Object).Handle(new CreateRootLedgerCommand(TenantId, "ROOT", "Root", "USD", UserId, "desc"), default)).Code.Should().Be(ledger.Code);
        (await new CreateChildLedgerHandler(hierarchyService.Object).Handle(new CreateChildLedgerCommand(ledger.Id, LedgerType.Department, "CHILD", "Child", UserId), default)).ParentLedgerId.Should().Be(ledger.Id);
        (await new GetLedgerByIdHandler(ledgerRepository.Object).Handle(new GetLedgerByIdQuery(ledger.Id), default)).Should().NotBeNull();
        (await new GetLedgerByIdHandler(ledgerRepository.Object).Handle(new GetLedgerByIdQuery(Guid.NewGuid()), default)).Should().BeNull();
        (await new GetLedgerByCodeHandler(ledgerRepository.Object).Handle(new GetLedgerByCodeQuery(TenantId, ledger.Code), default)).Should().NotBeNull();
        (await new GetLedgersByTenantHandler(ledgerRepository.Object).Handle(new GetLedgersByTenantQuery(TenantId), default)).Should().ContainSingle();
        (await new GetRootLedgersHandler(ledgerRepository.Object).Handle(new GetRootLedgersQuery(TenantId), default)).Should().ContainSingle();
        (await new GetLedgerChildrenHandler(ledgerRepository.Object).Handle(new GetLedgerChildrenQuery(ledger.Id), default)).Should().BeEmpty();
        (await new GetLedgerHierarchyHandler(hierarchyService.Object).Handle(new GetLedgerHierarchyQuery(ledger.Id, 1), default)).Code.Should().Be(ledger.Code);
        (await new GetLedgerAncestorsHandler(hierarchyService.Object).Handle(new GetLedgerAncestorsQuery(ledger.Id), default)).Should().ContainSingle();
        (await new GetLedgerDescendantsHandler(closureRepository.Object, ledgerRepository.Object).Handle(new GetLedgerDescendantsQuery(ledger.Id, 1), default)).Should().ContainSingle();
        (await new GetVirtualLedgersHandler(ledgerRepository.Object).Handle(new GetVirtualLedgersQuery(TenantId), default)).Should().ContainSingle();

        (await new PostEntryHandler(entryService.Object).Handle(new PostEntryCommand(ledger.Id, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", UserId, "ext"), default)).Id.Should().Be(entry.Id);
        (await new PostTransferHandler(entryService.Object).Handle(new PostTransferCommand(ledger.Id, ledger.Id, 1m, BusinessDate, "Transfer", UserId), default)).DebitEntry.Should().NotBeNull();
        (await new ReverseEntryHandler(entryService.Object).Handle(new ReverseEntryCommand(entry.Id, "reason", UserId), default)).Id.Should().Be(entry.Id);
        (await new UpdateEntryHandler(entryService.Object).Handle(new UpdateEntryCommand(entry.Id, "updated", UserId, "notes"), default)).Id.Should().Be(entry.Id);
        (await new DeleteEntryHandler(entryService.Object).Handle(new DeleteEntryCommand(entry.Id, UserId), default)).Should().BeTrue();
        (await new GetEntryByIdHandler(entryRepository.Object).Handle(new GetEntryByIdQuery(entry.Id), default)).Should().NotBeNull();
        (await new GetEntryByIdHandler(entryRepository.Object).Handle(new GetEntryByIdQuery(nullLedgerEntry.Id), default))!.LedgerCode.Should().BeEmpty();
        (await new GetEntryByIdHandler(entryRepository.Object).Handle(new GetEntryByIdQuery(Guid.NewGuid()), default)).Should().BeNull();
        (await new GetEntriesByLedgerHandler(entryRepository.Object).Handle(new GetEntriesByLedgerQuery(ledger.Id), default)).Should().ContainSingle();
        (await new GetEntriesIncludingDescendantsHandler(entryRepository.Object).Handle(new GetEntriesIncludingDescendantsQuery(ledger.Id), default)).Should().ContainSingle();
        (await new GetVirtualLedgerEntriesHandler(virtualService.Object).Handle(new GetVirtualLedgerEntriesQuery(ledger.Id), default)).Should().ContainSingle();
        (await new GetLedgerBalanceHandler(rollupService.Object).Handle(new GetLedgerBalanceQuery(ledger.Id), default)).Should().Be(balance);
        (await new GetConsolidatedBalanceHandler(rollupService.Object).Handle(new GetConsolidatedBalanceQuery(ledger.Id), default)).Should().Be(balance);
        (await new GetHierarchicalRollupHandler(rollupService.Object).Handle(new GetHierarchicalRollupQuery(ledger.Id), default)).Should().Be(rollup);
        (await new GetCategoryRollupHandler(rollupService.Object).Handle(new GetCategoryRollupQuery(ledger.Id), default)).Should().ContainSingle();
        (await new GetPeriodRollupHandler(rollupService.Object).Handle(new GetPeriodRollupQuery(ledger.Id, BusinessDate, BusinessDate, PeriodGranularity.Month), default)).Should().ContainSingle();
    }

    [Fact]
    public async Task Controllers_CoverCQRSRoutesAndResultBranches()
    {
        var ledger = LedgerDto();
        var entry = LedgerEntryDto(ledger.Id);
        var tree = new LedgerTreeNode
        {
            Id = ledger.Id,
            Code = ledger.Code,
            Name = ledger.Name,
            Type = ledger.Type,
            Status = ledger.Status,
            CurrencyCode = ledger.CurrencyCode,
            Depth = 0
        };
        var balance = new LedgerBalance(ledger.Id, ledger.Code, ledger.Name, 1m, 2m, 1m, 2, "USD", DateTime.UtcNow);
        var rollup = new LedgerRollupNode { Id = ledger.Id, Code = ledger.Code, Name = ledger.Name, Type = ledger.Type, Depth = 0, OwnBalance = balance, ConsolidatedBalance = balance };
        var transfer = new TransferResult(entry, LedgerEntryDto(ledger.Id, EntryType.Credit));

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetLedgerByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
        sender.Setup(s => s.Send(It.Is<GetLedgerByIdQuery>(q => q.LedgerId == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((LedgerDto?)null);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerByCodeQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
        sender.Setup(s => s.Send(It.Is<GetLedgerByCodeQuery>(q => q.Code == "missing"), It.IsAny<CancellationToken>())).ReturnsAsync((LedgerDto?)null);
        sender.Setup(s => s.Send(It.IsAny<GetLedgersByTenantQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<GetRootLedgersQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerChildrenQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerHierarchyQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(tree);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerAncestorsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerDescendantsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<GetVirtualLedgersQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([ledger]);
        sender.Setup(s => s.Send(It.IsAny<CreateRootLedgerCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
        sender.Setup(s => s.Send(It.IsAny<CreateChildLedgerCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(ledger);

        sender.Setup(s => s.Send(It.IsAny<GetEntryByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        sender.Setup(s => s.Send(It.Is<GetEntryByIdQuery>(q => q.EntryId == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((LedgerEntryDto?)null);
        sender.Setup(s => s.Send(It.IsAny<GetEntriesByLedgerQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([entry]);
        sender.Setup(s => s.Send(It.IsAny<GetEntriesIncludingDescendantsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([entry]);
        sender.Setup(s => s.Send(It.IsAny<PostEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        sender.Setup(s => s.Send(It.IsAny<PostTransferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(transfer);
        sender.Setup(s => s.Send(It.IsAny<UpdateEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        sender.Setup(s => s.Send(It.IsAny<DeleteEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sender.Setup(s => s.Send(It.IsAny<ReverseEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        sender.Setup(s => s.Send(It.IsAny<GetLedgerBalanceQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(balance);
        sender.Setup(s => s.Send(It.IsAny<GetConsolidatedBalanceQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(balance);
        sender.Setup(s => s.Send(It.IsAny<GetHierarchicalRollupQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(rollup);
        sender.Setup(s => s.Send(It.IsAny<GetCategoryRollupQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([new CategoryRollup(EntryCategory.Expense, 1m, 0m, -1m, 1, 100m)]);
        sender.Setup(s => s.Send(It.IsAny<GetPeriodRollupQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([new PeriodRollup(BusinessDate, BusinessDate, "May 2026", 1m, 0m, -1m, -1m, 1)]);

        // SystemAdmin actor: tenant scoping is bypassed, matching the pre-hardening behavior.
        var actorAccessor = new Moq.Mock<GameGuild.Identity.Context.Actors.IActorContextAccessor>();
        actorAccessor.SetupGet(a => a.ActorContext).Returns(new GameGuild.Identity.Context.Actors.ActorContext
        {
            IsAuthenticated = true,
            ActorKind = GameGuild.Identity.Context.Actors.ActorKind.User,
            SubjectId = UserId.ToString(),
            TenantId = TenantId,
            Roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SystemAdmin" },
            Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        });

        var ledgers = new LedgersController(sender.Object, actorAccessor.Object);
        (await ledgers.GetById(ledger.Id, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetById(Guid.Empty, default)).Result.Should().BeOfType<NotFoundResult>();
        (await ledgers.GetByCode(TenantId, ledger.Code, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetByCode(TenantId, "missing", default)).Result.Should().BeOfType<NotFoundResult>();
        (await ledgers.GetByTenant(TenantId, LedgerStatus.Active, LedgerType.Root)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetRootLedgers(TenantId, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetChildren(ledger.Id, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetHierarchy(ledger.Id, 1)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetAncestors(ledger.Id, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetDescendants(ledger.Id, 1)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.GetVirtualLedgers(TenantId, default)).Result.Should().BeOfType<OkObjectResult>();
        (await ledgers.CreateRootLedger(new CreateRootLedgerRequest(TenantId, "ROOT", "Root", "USD", UserId, "desc", ["tag"]), default)).Result.Should().BeOfType<CreatedAtActionResult>();
        (await ledgers.CreateChildLedger(ledger.Id, new CreateChildLedgerRequest(LedgerType.Department, "CHILD", "Child", UserId, "desc", "USD", true, 1m, BusinessDate, BusinessDate.AddDays(1), ["tag"]), default)).Result.Should().BeOfType<CreatedAtActionResult>();

        var entries = new LedgerEntriesController(sender.Object, actorAccessor.Object);
        (await entries.GetEntryById(entry.Id, default)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetEntryById(Guid.Empty, default)).Result.Should().BeOfType<NotFoundResult>();
        (await entries.GetEntriesByLedger(ledger.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetEntriesWithDescendants(ledger.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.PostEntry(ledger.Id, new PostEntryRequest(EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", UserId, "ext", "counterparty", Guid.NewGuid(), "OPS", ["tag"]), default)).Result.Should().BeOfType<CreatedAtActionResult>();
        (await entries.PostTransfer(new PostTransferRequest(ledger.Id, ledger.Id, 1m, BusinessDate, "Transfer", UserId), default)).Result.Should().BeOfType<CreatedResult>();
        (await entries.UpdateEntry(entry.Id, new UpdateEntryRequest("updated", UserId, "note"), default)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.DeleteEntry(entry.Id, default)).Should().BeOfType<NoContentResult>();
        (await entries.ReverseEntry(entry.Id, new ReverseEntryRequest("reason", UserId), default)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetBalance(ledger.Id, BusinessDate)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetConsolidatedBalance(ledger.Id, BusinessDate)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetHierarchicalRollup(ledger.Id, BusinessDate, BusinessDate)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetCategoryRollup(ledger.Id, true, BusinessDate, BusinessDate)).Result.Should().BeOfType<OkObjectResult>();
        (await entries.GetPeriodRollup(ledger.Id, BusinessDate, BusinessDate, PeriodGranularity.Day, true)).Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Repositories_CoverQueriesMutationsSummariesAndDeleteBranches()
    {
        await using var db = CreateDbContext();
        var root = Root("REPO");
        var child = Ledger.CreateChild(root, LedgerType.Department, "REPO-CHILD", "Child", UserId);
        var virtualLedger = Ledger.CreateVirtual(TenantId, "REPO-VIRTUAL", "Virtual", "{}", [root.Id], UserId);
        Set(child, nameof(Ledger.UpdatedAt), DateTime.UtcNow);
        Set(virtualLedger, nameof(Ledger.UpdatedAt), DateTime.UtcNow);

        var ledgerRepository = new LedgerRepository(db);
        await ledgerRepository.AddAsync(root);
        await ledgerRepository.AddAsync(virtualLedger);

        (await ledgerRepository.GetByIdAsync(root.Id)).Should().NotBeNull();
        (await ledgerRepository.GetByCodeAsync(TenantId, "REPO")).Should().NotBeNull();
        (await ledgerRepository.GetBySlugAsync(TenantId, "repo")).Should().NotBeNull();
        (await ledgerRepository.GetByTenantAsync(TenantId)).Should().HaveCount(3);
        (await ledgerRepository.GetRootLedgersAsync(TenantId)).Should().ContainSingle(l => l.Id == root.Id);
        (await ledgerRepository.GetChildrenAsync(root.Id)).Should().ContainSingle(l => l.Id == child.Id);
        (await ledgerRepository.GetVirtualLedgersAsync(TenantId)).Should().ContainSingle(l => l.Id == virtualLedger.Id);
        (await ledgerRepository.ExistsAsync(TenantId, "REPO")).Should().BeTrue();
        root.Update("Repo Updated", null, UserId);
        await ledgerRepository.UpdateAsync(root);
        (await ledgerRepository.GetByIdAsync(root.Id))!.Name.Should().Be("Repo Updated");

        var closureRepository = new LedgerClosureRepository(db);
        var selfRoot = LedgerClosure.CreateSelfReference(root);
        var selfChild = LedgerClosure.CreateSelfReference(child);
        var rootToChild = LedgerClosure.Create(root, child, 1);
        await closureRepository.AddAsync(selfRoot);
        await closureRepository.AddRangeAsync([selfChild, rootToChild]);
        (await closureRepository.GetAncestorsAsync(child.Id)).Should().ContainSingle(c => c.AncestorId == root.Id);
        (await closureRepository.GetDescendantsAsync(root.Id)).Should().ContainSingle(c => c.DescendantId == child.Id);
        (await closureRepository.GetDescendantsAsync(root.Id, 0)).Should().BeEmpty();
        (await closureRepository.GetDescendantIdsAsync(root.Id)).Should().Contain([root.Id, child.Id]);
        (await closureRepository.GetDescendantIdsAsync(root.Id, 0)).Should().ContainSingle(id => id == root.Id);
        (await closureRepository.GetAncestorIdsAsync(child.Id)).Should().Contain([child.Id, root.Id]);
        (await closureRepository.IsAncestorOfAsync(root.Id, child.Id)).Should().BeTrue();

        var entryRepository = new LedgerEntryRepository(db);
        var debit = PostedEntry(root, EntryType.Debit, EntryCategory.Expense, 10m, new DateOnly(2026, 1, 1));
        var credit = PostedEntry(child, EntryType.Credit, EntryCategory.Revenue, 25m, new DateOnly(2026, 2, 1), [root.Id]);
        var pending = LedgerEntry.Create(root, [], EntryType.Credit, EntryCategory.Revenue, 100m, new DateOnly(2026, 3, 1), "pending", UserId, "external-pending");
        await entryRepository.AddAsync(debit);
        await entryRepository.AddRangeAsync([credit, pending]);

        (await entryRepository.GetByIdAsync(debit.Id)).Should().NotBeNull();
        (await entryRepository.GetByExternalIdAsync("external-pending")).Should().NotBeNull();
        (await entryRepository.GetByLedgerAsync(root.Id)).Should().HaveCount(2);
        (await entryRepository.GetByLedgerAsync(root.Id, new DateTimeOffset(new DateTime(2026, 2, 1), TimeSpan.Zero), null)).Should().ContainSingle(e => e.Id == pending.Id);
        (await entryRepository.GetByLedgerAsync(root.Id, null, new DateTimeOffset(new DateTime(2026, 1, 31), TimeSpan.Zero))).Should().ContainSingle(e => e.Id == debit.Id);
        (await entryRepository.GetByLedgerIncludingDescendantsAsync(root.Id)).Should().Contain(e => e.Id == credit.Id);
        (await entryRepository.GetByLedgerIncludingDescendantsAsync(root.Id, new DateTimeOffset(new DateTime(2026, 2, 1), TimeSpan.Zero), new DateTimeOffset(new DateTime(2026, 2, 28), TimeSpan.Zero))).Should().ContainSingle(e => e.Id == credit.Id);
        (await entryRepository.GetNextSequenceNumberAsync(root.Id)).Should().Be(3);

        var directSummary = await entryRepository.GetBalanceSummaryAsync(root.Id);
        directSummary.TotalDebits.Should().Be(10m);
        directSummary.TotalCredits.Should().Be(0m);
        directSummary.EntryCount.Should().Be(1);
        var datedSummary = await entryRepository.GetBalanceSummaryAsync(root.Id, new DateTimeOffset(new DateTime(2026, 1, 31), TimeSpan.Zero));
        datedSummary.EntryCount.Should().Be(1);
        var emptySummary = await entryRepository.GetBalanceSummaryAsync(Guid.NewGuid());
        emptySummary.Should().Be((0m, 0m, 0));

        var descendantSummary = await entryRepository.GetBalanceSummaryIncludingDescendantsAsync(root.Id);
        descendantSummary.TotalCredits.Should().Be(25m);
        var datedDescendantSummary = await entryRepository.GetBalanceSummaryIncludingDescendantsAsync(root.Id, new DateTimeOffset(new DateTime(2026, 1, 31), TimeSpan.Zero));
        datedDescendantSummary.TotalCredits.Should().Be(0m);
        var emptyDescendantSummary = await entryRepository.GetBalanceSummaryIncludingDescendantsAsync(Guid.NewGuid());
        emptyDescendantSummary.Should().Be((0m, 0m, 0));

        debit.UpdateDescription("Updated", "notes", UserId);
        await entryRepository.UpdateAsync(debit);
        (await entryRepository.GetByIdAsync(debit.Id))!.Description.Should().Be("Updated");
        await entryRepository.DeleteAsync(pending);
        (await entryRepository.GetByIdAsync(pending.Id)).Should().BeNull();

        await closureRepository.DeleteByDescendantAsync(child.Id);
        (await closureRepository.GetAncestorIdsAsync(child.Id)).Should().BeEmpty();
        await closureRepository.DeleteSubtreeAsync(root.Id);
        (await closureRepository.GetDescendantIdsAsync(root.Id)).Should().BeEmpty();

        await ledgerRepository.DeleteAsync(virtualLedger);
        (await ledgerRepository.GetByIdAsync(virtualLedger.Id)).Should().BeNull();
    }

    [Fact]
    public void RecordsRequestsModuleAndServiceCollection_CoverConstructorsAndExtensions()
    {
        var ledgerId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var filter = new VirtualLedgerFilter([ledgerId], [LedgerType.Root], [EntryCategory.Expense], ["ops"], 1m, 100m, "Acme", true);

        object[] records =
        [
            new CreateRootLedgerCommand(TenantId, "ROOT", "Root", "USD", UserId, "desc", ["tag"]),
            new CreateChildLedgerCommand(ledgerId, LedgerType.Department, "CHILD", "Child", UserId, "desc", "USD", true, 100m, BusinessDate, BusinessDate.AddDays(1), ["tag"]),
            new CreateVirtualLedgerCommand(TenantId, "VIRT", "Virtual", filter, UserId, "desc"),
            new UpdateLedgerCommand(ledgerId, "Updated", UserId, "desc", ["tag"]),
            new ChangeLedgerStatusCommand(ledgerId, LedgerStatus.Archived, UserId),
            new MoveLedgerCommand(ledgerId, Guid.NewGuid(), UserId),
            new SetLedgerBudgetCommand(ledgerId, 1m, UserId),
            new DeleteLedgerCommand(ledgerId, UserId),
            new PostEntryCommand(ledgerId, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", UserId, "ext", "counterparty", budgetId, "OPS", ["tag"]),
            new PostTransferCommand(ledgerId, Guid.NewGuid(), 1m, BusinessDate, "Transfer", UserId),
            new PostBatchEntriesCommand([new CreateEntryRequest(ledgerId, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry")], UserId),
            new ReverseEntryCommand(entryId, "reason", UserId),
            new ReconcileEntryCommand(entryId, UserId, "ext"),
            new UpdateEntryCommand(entryId, "Updated", UserId, "notes"),
            new DeleteEntryCommand(entryId, UserId),
            new RefreshLedgerStatsCommand(ledgerId),
            new RefreshSubtreeStatsCommand(ledgerId),
            new ChangeEntryStatusCommand(entryId, EntryStatus.Reconciled, UserId, "reason"),
            new UpdateVirtualLedgerFilterCommand(ledgerId, filter, UserId),
            new DeleteVirtualLedgerCommand(ledgerId, UserId),
            new GetLedgerByIdQuery(ledgerId),
            new GetLedgerByCodeQuery(TenantId, "ROOT"),
            new GetLedgersByTenantQuery(TenantId, LedgerStatus.Active, LedgerType.Root, 1, 10),
            new GetRootLedgersQuery(TenantId),
            new GetChildLedgersQuery(ledgerId),
            new GetLedgerChildrenQuery(ledgerId),
            new GetLedgerHierarchyQuery(ledgerId, 2),
            new GetLedgerAncestorsQuery(ledgerId),
            new GetLedgerDescendantsQuery(ledgerId, 2),
            new GetVirtualLedgersQuery(TenantId),
            new SearchLedgersQuery(TenantId, "root", LedgerType.Root, LedgerStatus.Active, ["tag"], 1, 10),
            new GetEntryByIdQuery(entryId),
            new GetEntryByReferenceQuery(TenantId, "TXN"),
            new GetLedgerEntriesQuery(ledgerId, EntryStatus.Posted, BusinessDate, BusinessDate, 1, 10),
            new GetEntriesByLedgerQuery(ledgerId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow),
            new GetLedgerEntriesWithDescendantsQuery(ledgerId, EntryStatus.Posted, BusinessDate, BusinessDate, 1, 10),
            new GetEntriesIncludingDescendantsQuery(ledgerId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow),
            new GetVirtualLedgerEntriesQuery(ledgerId, BusinessDate, BusinessDate, 1, 10),
            new SearchEntriesQuery(TenantId, "entry", ledgerId, true, EntryType.Debit, EntryCategory.Expense, EntryStatus.Posted, 1m, 100m, BusinessDate, BusinessDate, ["tag"], 1, 10),
            new GetLedgerBalanceQuery(ledgerId, BusinessDate),
            new GetConsolidatedBalanceQuery(ledgerId, BusinessDate),
            new GetHierarchicalRollupQuery(ledgerId, BusinessDate, BusinessDate),
            new GetCategoryRollupQuery(ledgerId, true, BusinessDate, BusinessDate),
            new GetPeriodRollupQuery(ledgerId, BusinessDate, BusinessDate, PeriodGranularity.Month, true),
            new GetVirtualLedgerBalanceQuery(ledgerId, BusinessDate),
            new CreateEntryRequest(ledgerId, EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", "ext", budgetId, "OPS", "Counterparty", ["tag"]),
            new CreateLedgerRequest("CODE", "Name", LedgerType.Root, "desc", "USD", ledgerId, true, 1m, BusinessDate, BusinessDate, ["tag"]),
            new UpdateLedgerRequest("Updated", "desc", LedgerStatus.Active, 1m, BusinessDate, BusinessDate, ["tag"]),
            filter,
            new LedgerBalance(ledgerId, "CODE", "Name", 1m, 2m, 1m, 1, "USD", DateTime.UtcNow, true),
            new LedgerSummary(ledgerId, "CODE", "Name", LedgerType.Root, LedgerStatus.Active, "USD", "/CODE", 0, null, 1m, 1, 0, DateTime.UtcNow, DateTime.UtcNow),
            new CategoryRollup(EntryCategory.Expense, 1m, 2m, 1m, 3, 50m),
            new PeriodRollup(BusinessDate, BusinessDate, "May 2026", 1m, 2m, 1m, 1m, 3),
            LedgerEntryDto(ledgerId),
            LedgerDto(),
            new GameGuild.Finance.Ledgers.Models.PagedResult<LedgerDto>([LedgerDto()], 1, 1, 50, 1),
            new TransferResult(LedgerEntryDto(ledgerId), LedgerEntryDto(ledgerId, EntryType.Credit)),
            new VirtualFilterSpec("category", [EntryCategory.Expense], [ledgerId], true, 1m, 100m, BusinessDate, BusinessDate, ["tag"]),
            new BatchEntryRequest(ledgerId, EntryType.Debit, 1m, "Entry", DateTimeOffset.UtcNow, EntryCategory.Expense, "ref", "ext", new Dictionary<string, object> { ["k"] = "v" }),
            new CreateRootLedgerRequest(TenantId, "ROOT", "Root", "USD", UserId, "desc", ["tag"]),
            new CreateChildLedgerRequest(LedgerType.Department, "CHILD", "Child", UserId, "desc", "USD", true, 1m, BusinessDate, BusinessDate, ["tag"]),
            new PostEntryRequest(EntryType.Debit, EntryCategory.Expense, 1m, BusinessDate, "Entry", UserId, "ext", "counterparty", budgetId, "OPS", ["tag"]),
            new PostTransferRequest(ledgerId, Guid.NewGuid(), 1m, BusinessDate, "Transfer", UserId),
            new UpdateEntryRequest("Updated", UserId, "notes"),
            new ReverseEntryRequest("Reason", UserId),
        ];

        foreach (var record in records)
        {
            record.ToString().Should().NotBeNullOrWhiteSpace();
            record.GetHashCode().Should().NotBe(0);
            record.Equals(record).Should().BeTrue();
        }

        var treeNode = new LedgerTreeNode
        {
            Id = ledgerId,
            Code = "TREE",
            Name = "Tree",
            Type = LedgerType.Root,
            Status = LedgerStatus.Active,
            CurrencyCode = "USD",
            Depth = 0,
            NetBalance = 1m,
            EntryCount = 1,
            Children = [new LedgerTreeNode { Id = Guid.NewGuid(), Code = "CHILD", Name = "Child", Type = LedgerType.Department, Status = LedgerStatus.Active, CurrencyCode = "USD", Depth = 1 }]
        };
        treeNode.Children.Should().ContainSingle();

        var rollupNode = new LedgerRollupNode
        {
            Id = ledgerId,
            Code = "ROLL",
            Name = "Roll",
            Type = LedgerType.Root,
            Depth = 0,
            OwnBalance = new LedgerBalance(ledgerId, "ROLL", "Roll", 1m, 0m, -1m, 1, "USD", DateTime.UtcNow),
            ConsolidatedBalance = new LedgerBalance(ledgerId, "ROLL", "Roll", 1m, 1m, 0m, 2, "USD", DateTime.UtcNow),
            Children = []
        };
        rollupNode.Children.Should().BeEmpty();

        LedgerModule.ModuleName.Should().Be("Finance.Ledgers");
        LedgerModule.ModuleVersion.Should().Be("1.0.0");

        var services = new ServiceCollection();
        services.AddLedgerModule().Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(ILedgerRepository) && d.ImplementationType == typeof(LedgerRepository));
        services.Should().Contain(d => d.ServiceType == typeof(ILedgerEntryService) && d.ImplementationType == typeof(LedgerEntryService));

        var options = new DbContextOptionsBuilder<FinanceLedgerTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new FinanceLedgerTestDbContext(options);
        var builder = new ModelBuilder();
        builder.ApplyLedgerModuleConfigurations().Should().BeSameAs(builder);
        Action ensureSchema = () => context.EnsureFinanceSchemaCreated();
        ensureSchema.Should().Throw<InvalidOperationException>();

        var modelConfiguration = new FinanceLedgersModelConfiguration();
        var configurationBuilder = new ModelBuilder();
        modelConfiguration.Configure(configurationBuilder);
        configurationBuilder.Model.FindEntityType(typeof(Ledger)).Should().NotBeNull();
    }

    private static Ledger Root(string code, string name = "Ledger")
    {
        var ledger = Ledger.CreateRoot(TenantId, code, name, "USD", UserId, "desc");
        Set(ledger, nameof(Ledger.UpdatedAt), DateTime.UtcNow);
        return ledger;
    }

    private static LedgerEntry PostedEntry(
        Ledger ledger,
        EntryType type = EntryType.Debit,
        EntryCategory category = EntryCategory.Expense,
        decimal amount = 1m,
        DateOnly? transactionDate = null,
        IEnumerable<Guid>? ancestorLedgerIds = null)
    {
        var entry = LedgerEntry.Create(
            ledger,
            ancestorLedgerIds ?? [],
            type,
            category,
            amount,
            transactionDate ?? BusinessDate,
            $"{category} entry",
            UserId);
        entry.Post(UserId);
        return entry;
    }

    private static LedgerDto LedgerDto() => new(
        Id: Guid.NewGuid(),
        TenantId: TenantId,
        Code: "ROOT",
        Name: "Root",
        Description: "desc",
        Slug: "root",
        Type: LedgerType.Root,
        Status: LedgerStatus.Active,
        CurrencyCode: "USD",
        FiscalYearStartMonth: 1,
        ParentLedgerId: null,
        ParentLedgerCode: null,
        HierarchyPath: "/ROOT",
        HierarchyDepth: 0,
        IsShared: true,
        AllowDirectEntries: true,
        BudgetLimit: 100m,
        ProjectStartDate: BusinessDate,
        ProjectEndDate: BusinessDate.AddDays(1),
        CachedNetBalance: 1m,
        CachedEntryCount: 1,
        StatsCalculatedAt: DateTime.UtcNow,
        Tags: ["tag"],
        ChildCount: 0,
        CreatedByUserId: UserId,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    private static LedgerEntryDto LedgerEntryDto(Guid ledgerId, EntryType type = EntryType.Debit) => new(
        Id: Guid.NewGuid(),
        ReferenceNumber: "TXN-20260530-ABCDEF12",
        LedgerId: ledgerId,
        LedgerCode: "ROOT",
        LedgerPath: "/ROOT",
        Type: type,
        Status: EntryStatus.Posted,
        Category: type == EntryType.Debit ? EntryCategory.Expense : EntryCategory.Revenue,
        Amount: 1m,
        CurrencyCode: "USD",
        OriginalAmount: 2m,
        OriginalCurrencyCode: "BRL",
        TransactionDate: BusinessDate,
        PostingDate: BusinessDate,
        Description: "Entry",
        Notes: "notes",
        CounterpartyName: "Counterparty",
        ExternalReferenceId: "external",
        TransferPairEntryId: null,
        ReversesEntryId: null,
        ReversedByEntryId: null,
        BudgetId: Guid.NewGuid(),
        BudgetCategoryCode: "OPS",
        Tags: ["tag"],
        CreatedByUserId: UserId,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    private static FinanceLedgerTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceLedgerTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinanceLedgerTestDbContext(options);
    }

    private static void Set<T>(object target, string propertyName, T value)
    {
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }

    private sealed class FakeLedgerRepository(params Ledger[] ledgers) : ILedgerRepository
    {
        public List<Ledger> AddedLedgers { get; } = [];
        public List<Ledger> UpdatedLedgers { get; } = [];
        private readonly Dictionary<Guid, Ledger> _ledgers = ledgers.ToDictionary(l => l.Id);

        public void Seed(params Ledger[] ledgersToSeed)
        {
            foreach (var ledger in ledgersToSeed)
            {
                _ledgers[ledger.Id] = ledger;
            }
        }

        public Task<Ledger?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_ledgers.GetValueOrDefault(id));

        public Task<Ledger?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default) =>
            Task.FromResult(_ledgers.Values.FirstOrDefault(l => l.TenantId == tenantId && l.Code == code));

        public Task<Ledger?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct = default) =>
            Task.FromResult(_ledgers.Values.FirstOrDefault(l => l.TenantId == tenantId && l.Slug == slug));

        public Task<IReadOnlyList<Ledger>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Ledger>>(_ledgers.Values.Where(l => l.TenantId == tenantId).OrderBy(l => l.Code).ToList());

        public Task<IReadOnlyList<Ledger>> GetRootLedgersAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Ledger>>(_ledgers.Values.Where(l => l.TenantId == tenantId && l.ParentLedgerId is null && l.Type != LedgerType.Virtual).OrderBy(l => l.Code).ToList());

        public Task<IReadOnlyList<Ledger>> GetChildrenAsync(Guid parentLedgerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Ledger>>(_ledgers.Values.Where(l => l.ParentLedgerId == parentLedgerId).OrderBy(l => l.Code).ToList());

        public Task<IReadOnlyList<Ledger>> GetVirtualLedgersAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Ledger>>(_ledgers.Values.Where(l => l.TenantId == tenantId && l.Type == LedgerType.Virtual).OrderBy(l => l.Code).ToList());

        public Task<bool> ExistsAsync(Guid tenantId, string code, CancellationToken ct = default) =>
            Task.FromResult(_ledgers.Values.Any(l => l.TenantId == tenantId && l.Code == code));

        public Task AddAsync(Ledger ledger, CancellationToken ct = default)
        {
            _ledgers[ledger.Id] = ledger;
            AddedLedgers.Add(ledger);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Ledger ledger, CancellationToken ct = default)
        {
            _ledgers[ledger.Id] = ledger;
            UpdatedLedgers.Add(ledger);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Ledger ledger, CancellationToken ct = default)
        {
            _ledgers.Remove(ledger.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLedgerClosureRepository : ILedgerClosureRepository
    {
        public List<LedgerClosure> AddedClosures { get; } = [];
        public List<Guid> DeletedDescendants { get; } = [];
        public Dictionary<Guid, IReadOnlyList<LedgerClosure>> AncestorsByDescendant { get; } = [];
        public Dictionary<Guid, IReadOnlyList<LedgerClosure>> DescendantsByAncestor { get; } = [];
        public Dictionary<Guid, IReadOnlyList<Guid>> AncestorIdsByDescendant { get; } = [];
        public Dictionary<Guid, IReadOnlyList<Guid>> DescendantIdsByAncestor { get; } = [];
        public bool IsAncestorOverride { get; set; }

        public Task AddAsync(LedgerClosure closure, CancellationToken ct = default)
        {
            AddedClosures.Add(closure);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<LedgerClosure> closures, CancellationToken ct = default)
        {
            AddedClosures.AddRange(closures);
            return Task.CompletedTask;
        }

        public Task DeleteByDescendantAsync(Guid descendantLedgerId, CancellationToken ct = default)
        {
            DeletedDescendants.Add(descendantLedgerId);
            return Task.CompletedTask;
        }

        public Task DeleteSubtreeAsync(Guid rootLedgerId, CancellationToken ct = default)
        {
            DeletedDescendants.Add(rootLedgerId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LedgerClosure>> GetAncestorsAsync(Guid descendantLedgerId, CancellationToken ct = default) =>
            Task.FromResult(AncestorsByDescendant.GetValueOrDefault(descendantLedgerId) ?? []);

        public Task<IReadOnlyList<LedgerClosure>> GetDescendantsAsync(Guid ancestorLedgerId, int? maxDepth = null, CancellationToken ct = default) =>
            Task.FromResult(DescendantsByAncestor.GetValueOrDefault(ancestorLedgerId) ?? []);

        public Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid descendantLedgerId, CancellationToken ct = default) =>
            Task.FromResult(AncestorIdsByDescendant.GetValueOrDefault(descendantLedgerId) ?? [descendantLedgerId]);

        public Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Guid ancestorLedgerId, int? maxDepth = null, CancellationToken ct = default) =>
            Task.FromResult(DescendantIdsByAncestor.GetValueOrDefault(ancestorLedgerId) ?? [ancestorLedgerId]);

        public Task<bool> IsAncestorOfAsync(Guid ancestorLedgerId, Guid descendantLedgerId, CancellationToken ct = default) =>
            Task.FromResult(IsAncestorOverride);
    }

    private sealed class FakeLedgerEntryRepository : ILedgerEntryRepository
    {
        public List<LedgerEntry> Entries { get; } = [];

        public void Seed(params LedgerEntry[] entries)
        {
            foreach (var entry in entries)
            {
                if (Entries.All(e => e.Id != entry.Id))
                {
                    Entries.Add(entry);
                }
            }
        }

        public Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

        public Task<LedgerEntry?> GetByExternalIdAsync(string externalId, CancellationToken ct = default) =>
            Task.FromResult(Entries.FirstOrDefault(e => e.ExternalReferenceId == externalId));

        public Task AddAsync(LedgerEntry entry, CancellationToken ct = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default)
        {
            Entries.AddRange(entries);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(LedgerEntry entry, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(LedgerEntry entry, CancellationToken ct = default)
        {
            Entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LedgerEntry>> GetByLedgerAsync(Guid ledgerId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LedgerEntry>>(FilterDates(Entries.Where(e => e.LedgerId == ledgerId), fromDate, toDate).OrderBy(e => e.TransactionDate).ToList());

        public Task<IReadOnlyList<LedgerEntry>> GetByLedgerIncludingDescendantsAsync(Guid ledgerId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LedgerEntry>>(FilterDates(Entries.Where(e => e.LedgerId == ledgerId || e.ParentLedgerIds.Contains(ledgerId)), fromDate, toDate).OrderBy(e => e.TransactionDate).ToList());

        public Task<int> GetNextSequenceNumberAsync(Guid ledgerId, CancellationToken ct = default) =>
            Task.FromResult(Entries.Count(e => e.LedgerId == ledgerId) + 1);

        public Task<(decimal TotalCredits, decimal TotalDebits, int EntryCount)> GetBalanceSummaryAsync(Guid ledgerId, DateTimeOffset? asOfDate = null, CancellationToken ct = default) =>
            Task.FromResult(Summarize(FilterDates(Entries.Where(e => e.LedgerId == ledgerId), null, asOfDate)));

        public Task<(decimal TotalCredits, decimal TotalDebits, int EntryCount)> GetBalanceSummaryIncludingDescendantsAsync(Guid ledgerId, DateTimeOffset? asOfDate = null, CancellationToken ct = default) =>
            Task.FromResult(Summarize(FilterDates(Entries.Where(e => e.LedgerId == ledgerId || e.ParentLedgerIds.Contains(ledgerId)), null, asOfDate)));

        private static IEnumerable<LedgerEntry> FilterDates(IEnumerable<LedgerEntry> entries, DateTimeOffset? fromDate, DateTimeOffset? toDate)
        {
            if (fromDate.HasValue)
            {
                var from = DateOnly.FromDateTime(fromDate.Value.UtcDateTime);
                entries = entries.Where(e => e.TransactionDate >= from);
            }

            if (toDate.HasValue)
            {
                var to = DateOnly.FromDateTime(toDate.Value.UtcDateTime);
                entries = entries.Where(e => e.TransactionDate <= to);
            }

            return entries;
        }

        private static (decimal TotalCredits, decimal TotalDebits, int EntryCount) Summarize(IEnumerable<LedgerEntry> entries)
        {
            var posted = entries.Where(e => e.Status == EntryStatus.Posted && e.ReversedByEntryId is null).ToList();
            return (
                posted.Where(e => e.Type == EntryType.Credit).Sum(e => e.Amount),
                posted.Where(e => e.Type == EntryType.Debit).Sum(e => e.Amount),
                posted.Count);
        }
    }

    private sealed class FinanceLedgerTestDbContext(DbContextOptions<FinanceLedgerTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyLedgerModuleConfigurations();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
