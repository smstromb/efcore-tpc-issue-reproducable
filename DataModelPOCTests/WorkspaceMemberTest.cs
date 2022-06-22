using DataModelPOC.Data;
using static DataModelPOC.Data.CareswitchDbContext;

namespace DataModelPOCTests;

public class WorkspaceMemberTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CareswitchDbContext _dbContext;

    private readonly ApplicationUser _user1;
    private readonly ApplicationUser _user2;
    private readonly ApplicationUser _user3;
    private readonly ApplicationUser _user4;
    private readonly Workspace _workspace1;
    private readonly Workspace _workspace2;
    private readonly Employee _workspace1Admin;
    private readonly Employee _workspace1Employee;
    private readonly Employee _workspace2Admin;
    private readonly Member _workspace1Member;
    private readonly Member _workspace2Member;
    private readonly CareRecipient _workspace1CareRecipient;

    public WorkspaceMemberTest(DatabaseFixture fixture)
    {
        _dbFixture = fixture;
        _dbContext = _dbFixture.CreateContextForSQLite();

        _user1 = new ApplicationUser(phoneNumber: "+19194141214");
        _user2 = new ApplicationUser(phoneNumber: "+19196567981");
        _user3 = new ApplicationUser(phoneNumber: "+19194698689");
        _user4 = new ApplicationUser(phoneNumber: "+19195555555");

        _dbContext.ApplicationUsers.AddRange(new[] { _user1, _user2, _user3, _user4 });

        _workspace1 = new Workspace(name: "My Workspace");
        // This is a stub for our identity provider
        _dbContext.SetWorkspace(_workspace1.Id);
        _dbContext.Workspaces.Add(_workspace1);

        _workspace1Admin = new Employee(name: "Spencer Strombotne", role: "Admin", specialEmployeeField: "Foo")
        {
            ApplicationUser = _user1
        };
        _workspace1CareRecipient = new CareRecipient(name: "Bob Sagat", role: "Guest", specialCareRecipientField: "Foo")
        {
            ApplicationUser = _user2
        };
        _workspace1Employee = new Employee(name: "Jeff Bezos", role: "Staff", specialEmployeeField: "Foo")
        {
            ApplicationUser = _user3,
            EmployeeInvitation = new EmployeeInvitation(message: "Get your i9 done, Bezos!")
            {
                EnrollInPayroll = true
            }
        };
        _workspace1Member = new Member(name: "Jill Sagat", role: "Guest", specialMemberField: "Foo")
        {
            ApplicationUser = _user4
        };

        _dbContext.WorkspaceMembers.AddRange(new List<WorkspaceMember> { _workspace1Admin, _workspace1CareRecipient, _workspace1Employee, _workspace1Member });
        _dbContext.SaveChanges();

        _workspace2 = new Workspace(name: "Workspace 2");

        _dbContext.SetWorkspace(_workspace2.Id);

        _dbContext.Workspaces.Add(_workspace2);

        _workspace2Admin = new Employee(name: "Spencer Strombotne", role: "Admin", specialEmployeeField: "Foo")
        {
            ApplicationUser = _user1,
        };
        _workspace2Member = new Member(name: "Bob Sagat", role: "Guest", specialMemberField: "Foo")
        {
            ApplicationUser = _user2,
            Invitation = new Invitation(message: "Welcome!")
        };

        _dbContext.WorkspaceMembers.AddRange(new List<WorkspaceMember> { _workspace2Admin, _workspace2Member });
        _dbContext.SaveChanges();

        _dbContext.ChangeTracker.Clear(); // Simulate a fresh context
    }

    [Fact]
    public void Scenario_Create_Workspace()
    {
        var user = _dbContext.ApplicationUsers.Single(u => u.Id == _user1.Id);

        var newWorkspace = new Workspace("My New Workspace");
        var newWorkspaceAdmin = new Employee(name: "Spencer Strombotne", role: "Admin", specialEmployeeField: "Foo")
        {
            ApplicationUser = user,
            Workspace = newWorkspace // We have to handle this because we're ignoring the WorkspaceId filtering middleware
        };

        _dbContext.WorkspaceMembers.Add(newWorkspaceAdmin);
        _dbContext.SaveChangesWithoutMiddleware();

        _dbContext.ChangeTracker.Clear(); // Simulate a fresh context

        // throws if not found
        _dbContext.Workspaces.Single(w => w.Id == newWorkspace.Id);
    }

    [Fact]
    public void Scenario_Fetch_Workspace_Members()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        List<WorkspaceMember> workspaceMembers = _dbContext.WorkspaceMembers.ToList();
        Assert.Equal(4, workspaceMembers.Count);

        var NumEmployees = workspaceMembers.Where(m => m is Employee).Count();
        Assert.Equal(2, NumEmployees);

        var NumMembers = workspaceMembers.Where(m => m is Member).Count();
        Assert.Equal(1, NumMembers);

        var NumCareRecipients = workspaceMembers.Where(m => m is CareRecipient).Count();
        Assert.Equal(1, NumCareRecipients);
    }

    [Fact]
    public void Scenario_Fetch_Employees()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        List<Employee> employees = _dbContext.Employees.ToList();
        Assert.Equal(2, employees.Count);
    }

    [Fact]
    public void Scenario_Fetch_Members()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        List<Member> members = _dbContext.Members.ToList();
        Assert.Single(members);
    }

    [Fact]
    public void Scenario_Fetch_CareRecipients()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        List<CareRecipient> careRecipients = _dbContext.CareRecipients.ToList();
        Assert.Single(careRecipients);
    }

    [Fact]
    public void Scenario_Invite_Existing_User()
    {
        _dbContext.SetWorkspace(_workspace2.Id);

        var existingUser = _dbContext.ApplicationUsers.Single(u => u.Id == _user3.Id);
        var newEmployee = new Employee(name: "Jeff Bezos", role: "Staff", specialEmployeeField: "Foo")
        {
            ApplicationUser = existingUser,
            EmployeeInvitation = new EmployeeInvitation("Seriously, get that i9 in")
            {
                EnrollInPayroll = true
            },
        };

        _dbContext.WorkspaceMembers.Add(newEmployee);
        _dbContext.SaveChanges();

        _dbContext.ChangeTracker.Clear(); // Simulate a fresh context

        // throws if not found
        _dbContext.WorkspaceMembers.Single(w => w.Id == newEmployee.Id);
    }

    [Fact]
    public void Scenario_Invite_New_User()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        var newUser = new ApplicationUser(phoneNumber: "+19191231234");
        var newMember = new Member(name: "Elon Musk", role: "Guest", specialMemberField: "Foo")
        {
            ApplicationUser = newUser,
            Invitation = new Invitation("Where's that new Roadster?")
        };

        _dbContext.WorkspaceMembers.Add(newMember);
        _dbContext.SaveChanges();

        // throws if not found
        _dbContext.WorkspaceMembers.Single(w => w.Id == newMember.Id);
    }

}
