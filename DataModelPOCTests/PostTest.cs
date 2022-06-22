using DataModelPOC.Data;
using Microsoft.EntityFrameworkCore;
using static DataModelPOC.Data.CareswitchDbContext;

namespace DataModelPOCTests;

public class PostTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CareswitchDbContext _dbContext;

    private readonly EmployeePost _employeePost;
    private readonly MemberPost _memberPost;
    private readonly CareRecipientPost _careRecipientPost;
    private readonly Feed _feed;

    private readonly ApplicationUser _user1;
    private readonly ApplicationUser _user2;
    private readonly ApplicationUser _user3;
    private readonly ApplicationUser _user4;
    private readonly Workspace _workspace1;
    private readonly Employee _workspace1Admin;
    private readonly Employee _workspace1Employee;
    private readonly Member _workspace1Member;
    private readonly CareRecipient _workspace1CareRecipient;

    public PostTest(DatabaseFixture fixture)
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

        _employeePost = new EmployeePost(body: "What's going on?")
        {
            Employee = _workspace1Employee
        };
        _memberPost = new MemberPost(body: "Nothing much, you?")
        {
            Member = _workspace1Member
        };
        _careRecipientPost = new CareRecipientPost(body: "...")
        {
            CareRecipient = _workspace1CareRecipient
        };

        _feed = new Feed()
        {
            CareRecipient = _workspace1CareRecipient,
            Posts = new List<Post>
            {
                _employeePost,
                _memberPost,
                _careRecipientPost
            }
        };

        _dbContext.Feeds.Add(_feed);

        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear(); // Simulate a fresh context
    }

    [Fact]
    public void Scenario_Get_Feed()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        // throws if not found
        _dbContext.Feeds.Single(f => f.Id == _feed.Id);
    }

    [Fact]
    public void Scenario_Get_Posts()
    {
        Feed fetchedFeed = _dbContext.Feeds.Include(f => f.Posts).Single(f => f.Id == _feed.Id);
        Assert.Equal(3, fetchedFeed.Posts.Count);

        int NumEmployeePosts = fetchedFeed.Posts.Where(p => p is EmployeePost).Count();
        Assert.Equal(1, NumEmployeePosts);

        int NumMemberPosts = fetchedFeed.Posts.Where(p => p is MemberPost).Count();
        Assert.Equal(1, NumMemberPosts);

        int NumCareRecipientPosts = fetchedFeed.Posts.Where(p => p is CareRecipientPost).Count();
        Assert.Equal(1, NumCareRecipientPosts);
    }

    [Fact]
    public void Scenario_Create_New_Post()
    {
        _dbContext.SetWorkspace(_workspace1.Id);
        _dbContext.Attach(_workspace1Admin);

        Feed fetchedFeed = _dbContext.Feeds.Single(f => f.Id == _feed.Id);

        var newPost = new EmployeePost(body: "Where's the care not from the last shift?")
        {
            Employee = _workspace1Admin,
            Feed = fetchedFeed
        };

        _dbContext.Posts.Add(newPost);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear(); // Simulate a fresh context

        Feed updatedFeed = _dbContext.Feeds.Include(f => f.Posts).Single(f => f.Id == _feed.Id);
        Assert.Equal(4, updatedFeed.Posts.Count);

        int NumEmployeePosts = updatedFeed.Posts.Where(p => p is EmployeePost).Count();
        Assert.Equal(2, NumEmployeePosts);

        int NumMemberPosts = updatedFeed.Posts.Where(p => p is MemberPost).Count();
        Assert.Equal(1, NumMemberPosts);

        int NumCareRecipientPosts = updatedFeed.Posts.Where(p => p is CareRecipientPost).Count();
        Assert.Equal(1, NumCareRecipientPosts);

    }

    [Fact]
    public void Scenario_Get_Posts_With_Author()
    {
        _dbContext.SetWorkspace(_workspace1.Id);

        List<Post> employeePosts = _dbContext.EmployeePosts.Where(p => p.Feed.Id == _feed.Id).Include(p => p.Employee).ToList<Post>();
        List<Post> memberPosts = _dbContext.MemberPosts.Where(p => p.Feed.Id == _feed.Id).Include(p => p.Member).ToList<Post>();
        List<Post> careRecipientPosts = _dbContext.CareRecipientPosts.Where(p => p.Feed.Id == _feed.Id).Include(p => p.CareRecipient).ToList<Post>();

        List<Post> posts = employeePosts.Concat(memberPosts).Concat(careRecipientPosts).ToList();

        foreach (Post post in posts)
        {
            switch (post)
            {
                case EmployeePost:
                    Assert.Equal(_employeePost.Employee.Name, (post as EmployeePost)?.Employee.Name);
                    break;
                case MemberPost:
                    Assert.Equal(_memberPost.Member.Name, (post as MemberPost)?.Member.Name);
                    break;
                case CareRecipientPost:
                    Assert.Equal(_careRecipientPost.CareRecipient.Name, (post as CareRecipientPost)?.CareRecipient.Name);
                    break;
            }
        }
    }



}
