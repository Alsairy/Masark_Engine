using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Masark.Infrastructure.Identity;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using BCrypt.Net;

namespace Masark.Infrastructure.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DatabaseSeeder(
            ApplicationDbContext context, 
            ILogger<DatabaseSeeder> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                await SeedRolesAsync();
                await SeedAdminUserAsync();
                await SeedPersonalityTypesAsync();
                await SeedQuestionsAsync();
                await SeedCareerClustersAsync();
                await SeedCareersAsync();
                await SeedPathwaysAsync();
                await SeedCareerPathwayMappingsAsync();
                await SeedPersonalityCareerMatchesAsync();
                await SeedCareerClusterRatingsAsync();
                await SeedReportElementsAsync();
                await SeedTieBreakerQuestionsAsync();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "ADMIN", "USER" };
            
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Description = $"{roleName} role",
                        TenantId = 1
                    };
                    
                    await _roleManager.CreateAsync(role);
                    _logger.LogInformation("Created role");
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var existingIdentityUser = await _userManager.FindByNameAsync("admin");
            if (existingIdentityUser != null)
            {
                _logger.LogInformation("Identity admin user already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding admin user...");

            var identityUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@masark.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                TenantId = 1,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(identityUser, "Admin123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(identityUser, "ADMIN");
                _logger.LogInformation("Created Identity admin user with username: admin");
            }
            else
            {
                _logger.LogError("Failed to create Identity admin user");
            }

            if (!await _context.AdminUsers.AnyAsync())
            {
                var adminUser = new AdminUser(
                    "admin",
                    "admin@masark.com",
                    BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    1 // Default tenant ID
                );

                adminUser.UpdateProfile("Admin", "User", "admin@masark.com");
                await _context.AdminUsers.AddAsync(adminUser);
                _logger.LogInformation("Added AdminUser entity with username: admin");
            }
        }

        private async Task SeedPersonalityTypesAsync()
        {
            if (await _context.PersonalityTypes.AnyAsync())
            {
                _logger.LogInformation("Personality types already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding personality types...");

            var personalityTypes = new List<PersonalityType>();

            var intj = new PersonalityType("INTJ", "The Architect", "المهندس المعماري", 1);
            intj.UpdateContent(
                "The Architect", "المهندس المعماري",
                "Imaginative and strategic thinkers, with a plan for everything.",
                "أنت شخص منطقي، واثق من أفكارك وفي قدرتك على تلبية أو تجاوز أهدافك. أنت شخص طموح في كل ما تفعله، ولديك الدافعية لتكون مبدعاً ومتفوقاً، ولديك حدس قوي عما هو ممكن وتحمل وجهة نظر عالمية. أنت مفكر استراتيجي، وتنظر فيما وراء المعلوم كي ترى روابط بين العناصر التي غالباً ما تكون مختلفة جداً. أنت تسعى إلى الكمال، كما أنك ناقد وكثير المطالب من نفسك ولا تهاب المعارضة. أنت شخص مركز وعازم على تحقيق رؤيتك في الحياة، وتعمل بلا كلل لإنتاج فكرة أو منتج لا تشوبه شائبة. أنت تميل إلى الاهتمام بتلبية أو تجاوز معاييرك العالية الخاصة أكثر من محاولة إرضاء الآخرين.",
                "Strategic thinking, Independence, Determination, Hard-working, Open-minded",
                "التفكير الاستراتيجي، الاستقلالية، التصميم، العمل الجاد، الانفتاح الذهني",
                "Arrogant, Judgmental, Overly analytical, Loathe highly structured environments, Clueless in romance",
                "متعجرف، يحكم على الآخرين، مفرط في التحليل، يكره البيئات شديدة التنظيم، لا يفهم الرومانسية"
            );
            personalityTypes.Add(intj);
            var intp = new PersonalityType("INTP", "The Thinker", "المفكر", 1);
            intp.UpdateContent(
                "The Thinker", "المفكر",
                "Innovative inventors with an unquenchable thirst for knowledge.",
                "أنت شخص مستقل، وفضولي وتحب الخصوصية، وقضاء الوقت بمفردك للتفكير في الأشياء من خلال استكشاف الموضوعات والمشاريع التي تهمك بشكل خاص جداً. أنت تفضل أن يكون لديك مجموعة من الأصدقاء المقربين ذوي الثقة، ونادراً ما تبدأ أنت بالأنشطة الاجتماعية. أنت تفضل الحصول على أقصى استفادة من عدد قليل من الأنشطة الاجتماعية المميزة من خلال المشاركة في عدد كبير من اللقاءات القصيرة والمركّزة. قد يكون لديك شغف حقيقي في العلوم أو الفنون وتستمتع بتعلم أشياء جديدة، أنت تقوم بتكوين علاقات سريعة ومفيدة، وتستمتع بالوصول إلى حلول جذرية للمشكلات، إلا أنك تشعر بالملل بسرعة، وتكره التكرار، وقد تجد صعوبة في شرح أفكارك ببساطة ووضوح للآخرين.",
                "Great analysts and abstract thinkers, Imaginative and original, Open-minded, Enthusiastic, Objective, Honest and straightforward",
                "محللون عظماء ومفكرون مجردون، خياليون وأصليون، منفتحون، متحمسون، موضوعيون، صادقون ومباشرون",
                "Very private and withdrawn, Insensitive, Absent-minded, Condescending, Loathe rules and guidelines, Second-guess themselves",
                "خاصون جداً ومنطوون، غير حساسين، شاردو الذهن، متعالون، يكرهون القواعد والإرشادات، يشككون في أنفسهم"
            );
            personalityTypes.Add(intp);
            var entj = new PersonalityType("ENTJ", "The Commander", "القائد", 1);
            entj.UpdateContent(
                "The Commander", "القائد",
                "Bold, imaginative and strong-willed leaders, always finding a way – or making one.",
                "أنت شخص واثق من نفسك وحازم، تتحدث عما يدور في ذهنك ويبدو أنك تثق في نفسك دائماً، كما إنك صادق ونزيه وبالتالي صريح جداً. لديك آراء قوية وعادة ما تكون قادراً على إقناع الآخرين بأن موقفك هو الصواب. أنت شخص ودود ومريح وتمثل مركز اهتمام الآخرين، وربما لديك مجموعة كبيرة من الأصدقاء. أنت تميل إلى أن تسأل الأسئلة المحفزة للتفكير، تحب أن تتعلم، ولكنك تملّ من التكرار. أنت شخص تخيلي وتحب أن تنظر إلى ما وراء الروتين اليومي لفهم حقيقة لماذا يعمل العالم بالطريقة التي يعمل بها، وتحتاج إلى تحديات جديدة ثابتة للبقاء مهتماً.",
                "Efficient, Energetic, Self-confident, Strong-willed, Strategic thinkers, Charismatic and inspiring",
                "فعالون، نشيطون، واثقون من أنفسهم، أقوياء الإرادة، مفكرون استراتيجيون، جذابون وملهمون",
                "Stubborn and dominant, Intolerant, Impatient, Arrogant, Poor handling of emotions, Cold and ruthless",
                "عنيدون ومهيمنون، غير متسامحين، غير صبورين، متعجرفون، سيئون في التعامل مع العواطف، باردون وقساة"
            );
            personalityTypes.Add(entj);
            var entp = new PersonalityType("ENTP", "The Debater", "المناقش", 1);
            entp.UpdateContent(
                "The Debater", "المناقش",
                "Smart and curious thinkers who cannot resist an intellectual challenge.",
                "أنت شخص ودود ومبدع وواثق في نفسك، ولديك الكثير من الأصدقاء والعلاقات ومن السهل جداً التعرف عليك. أنت تحب أن تتحدث وتريد أن تكون دائماً في دائرة الضوء. أنت تستمتع بتسلية الآخرين بقصصك الجذابة، كما أنك طريف وتمتلك روح دعابة عالية، إلا أن لديك مشكلة بسيطة في التكيف مع التغيير. أنت تفخر بنفسك وبقدرتك على رؤية الإمكانيات واستثمارها. يمكنك فهم الأفكار الجديدة بسرعة وتستمتع بالتعلم، ومع ذلك، أنت سريع التشتت وتشعر بالملل عند انتهاء التحدي في مشروع ما.",
                "Knowledgeable, Quick thinkers, Original, Excellent brainstormers, Charismatic, Energetic",
                "مطلعون، مفكرون سريعون، أصليون، ممتازون في العصف الذهني، جذابون، نشيطون",
                "Very argumentative, Insensitive, Intolerant, Can find it difficult to focus, Dislike practical matters",
                "جدليون جداً، غير حساسين، غير متسامحين، يجدون صعوبة في التركيز، لا يحبون الأمور العملية"
            );
            personalityTypes.Add(entp);
            var infj = new PersonalityType("INFJ", "The Advocate", "المدافع", 1);
            infj.UpdateContent(
                "The Advocate", "المدافع",
                "Quiet and mystical, yet very inspiring and tireless idealists.",
                "أنت شخص تميل إلى أن تكون عميقاً ودقيقاً ومبدعاً، كما أن اتجاهك في الحياة منقاد بقيمك الشخصية الدقيقة المحكمة. أنت شخص مفتون بالطرق الابتكارية للنظر إلى العالم، وأنت تستقي إلهامك من الابتكار وحل المشاكل. أنت شخص يعكس مثالاً لطيفاً وثابتاً، ولذا فأنت جيد في التأثير على الآخرين من أجل بناء التغيير الإيجابي في حياتهم الخاصة. يساعدك حدسك على اكتشاف معان وإمكانيات جديدة، ويمكنك من تطوير حياة داخلية متوازنة. أنت شخص متحفظ وتحب الخصوصية، وقادر على منح الحنان والرحمة للآخرين الذين تعرفهم جيداً. أنت تتخذ قراراتك بعناية، مع تخصيص الوقت للتفكير في كل نتيجة بشكل شامل قبل تنفيذ اختيارك.",
                "Creative, Insightful, Inspiring and convincing, Decisive, Determined, Passionate, Altruistic",
                "مبدعون، بصيرون، ملهمون ومقنعون، حاسمون، مصممون، شغوفون، إيثاريون",
                "Sensitive, Extremely private, Perfectionist, Always need to have a cause, Can burn out easily",
                "حساسون، خاصون جداً، كماليون، يحتاجون دائماً لقضية، يمكن أن يحترقوا بسهولة"
            );
            personalityTypes.Add(infj);
            var infp = new PersonalityType("INFP", "The Mediator", "الوسيط", 1);
            infp.UpdateContent(
                "The Mediator", "الوسيط",
                "Poetic, kind and altruistic people, always eager to help a good cause.",
                "أنت شخص حساس ومثالي، وتسعى للانسجام الداخلي، وأنت صديق مخلص ومتعاطف، تكرس نفسك للناس وللقضايا التي تهتم بها. أنت قد تبدو بارداً أو مستقلاً أحياناً، إلا أن لديك مشاعراً قوية وجياشة جداً. أنت تثق في ردود أفعالك الشخصية ونظرتك للأمور، وتستخدم منظومتك القيمية لتوجيه حياتك. أنت شخص فضولي نحو الاحتمالات، ويمكنك الاستمتاع بالعديد من المحاولات الإبداعية كما يمكنك أن تكون مفكراً مبدعاً، وتحب استخدام سماتك الشخصية واستثمارها في كل ما تفعله.",
                "Idealistic, Loyal and devoted, Hard-working, Creative, Passionate, Open-minded",
                "مثاليون، مخلصون ومتفانون، مجتهدون، مبدعون، شغوفون، منفتحو الذهن",
                "Too idealistic, Too altruistic, Impractical, Dislike dealing with data, Take things personally, Difficult to get to know",
                "مثاليون جداً، إيثاريون جداً، غير عمليين، لا يحبون التعامل مع البيانات، يأخذون الأمور شخصياً، صعبو المعرفة"
            );
            personalityTypes.Add(infp);
            var enfj = new PersonalityType("ENFJ", "The Protagonist", "البطل", 1);
            enfj.UpdateContent(
                "The Protagonist", "البطل",
                "Charismatic and inspiring leaders, able to mesmerize their listeners.",
                "أنت شخص ودود، واجتماعي ومتحدث، تستطيع تكوين صداقات بسهولة، وغالباً ما تكون محبوباً. أنت تهتم كثيراً بالأسرة والأصدقاء، وتعبر عن مشاعرك من خلال الكلمات والأفعال. أسلوبك جيد في الحديث؛ حيث لديك حجج وآراء قوية، تستطيع التعبير عنها بلباقة. أنت عاطفي جداً ولديك إحساس فطري عمّا يشعر به الآخرون. تزعجك وتوترك الصراعات بشكل كبير، لذلك تحاول بجد إسعاد الآخرين، وإعادة التوافق بين الأفراد المتنازعين. تكره المواجهة المباشرة مما يجعل خطابك لطيفاً، أو تتجنب أن تكون صادقاً تماماً إذا كان ذلك يساعد على الانسجام ويمنع جرح مشاعر الآخرين.",
                "Tolerant, Reliable, Charismatic, Altruistic, Natural leaders",
                "متسامحون، موثوقون، جذابون، إيثاريون، قادة طبيعيون",
                "Overly idealistic, Too selfless, Too sensitive, Fluctuating self-esteem, Struggle to make tough decisions",
                "مثاليون جداً، منكرون للذات جداً، حساسون جداً، تقدير الذات متقلب، يكافحون لاتخاذ قرارات صعبة"
            );
            personalityTypes.Add(enfj);
            var enfp = new PersonalityType("ENFP", "The Campaigner", "المناضل", 1);
            enfp.UpdateContent(
                "The Campaigner", "المناضل",
                "Enthusiastic, creative and sociable free spirits, who can always find a reason to smile.",
                "أنت شخص مبادر، ومتحمس وعفوي وفضولي، تحب التحدث مع الآخرين مما قد يكون لديك الكثير من الأصدقاء. وأنت شخص حيوي ونشط، ومنفتح على الخبرات الجديدة باستمرار. وتطرح الكثير من الأسئلة، ولديك خيال خصب، وتحب حل المشكلات بطرق خارجة عن المألوف. أنت شخص تحب مناقشة القضايا؛ خاصة حول المرح أو الإمكانيات المثيرة للاهتمام. أنت حساس وعاطفي، ولديك إدراك ورؤية عن الآخرين. ويعرف أصدقاؤك أنك عطوف ومخلص وتتفاعل مع ما يواجهون من مشكلات بعمق، حتى لو لم تظهر ذلك.",
                "Enthusiastic, Creative, Sociable, Energetic, Compassionate",
                "متحمسون، مبدعون، اجتماعيون، نشيطون، رحماء",
                "Poor practical skills, Find it difficult to focus, Overthink things, Get stressed easily, Highly emotional",
                "مهارات عملية ضعيفة، يجدون صعوبة في التركيز، يفكرون كثيراً، يتوترون بسهولة، عاطفيون جداً"
            );
            personalityTypes.Add(enfp);
            var istj = new PersonalityType("ISTJ", "The Logistician", "اللوجستي", 1);
            istj.UpdateContent(
                "The Logistician", "اللوجستي",
                "Practical and fact-minded, reliability cannot be doubted.",
                "أنت شخص هادئ، واقعي وعملي، يمكنك التواصل بأسلوب واضح وبسيط ومباشر. أنت شخص مراقب حريص، وتلاحظ التفاصيل التي تهمك أو تتعلق بك ولها ذكرى جيدة بتجاربك الماضية. أنت تفكر في الأشياء مليّاً قبل مشاركة أفكارك، وتتوخى الحذر حول التغيير. أنت شخص مسؤول وواثق، وتجتهد في بذل قصارى جهدك في كل موقف. أنت شخص منطقي وصاحب ضمير حي، وترغب في اتخاذ القرارات المعقولة والحفاظ على الأمور مرتبة. أنت شخص منظم ومنتج، ولديك قدرة كبيرة على التركيز وإنجاز الأمور. أنت تضع معايير عالية لنفسك وللآخرين، وتحب أن تُقيَّم عن طريق ميزاتك، وتكون عادلاً وثابتاً على المبادئ عند التعامل مع الآخرين.",
                "Honest and direct, Strong-willed and dutiful, Very responsible, Calm and practical, Create and enforce order",
                "صادقون ومباشرون، أقوياء الإرادة ومطيعون للواجب، مسؤولون جداً، هادئون وعمليون، ينشئون النظام ويطبقونه",
                "Stubborn, Insensitive, Always by the book, Judgmental, Often unreasonably blame themselves",
                "عنيدون، غير حساسين، دائماً حسب الكتاب، يحكمون على الآخرين، غالباً ما يلومون أنفسهم بلا مبرر"
            );
            personalityTypes.Add(istj);
            var isfj = new PersonalityType("ISFJ", "The Protector", "الحامي", 1);
            isfj.UpdateContent(
                "The Protector", "الحامي",
                "Warm-hearted and dedicated, always ready to protect their loved ones.",
                "أنت شخص هادئ وجاد ومجتهد. أنت عملي وواقعي، وتهتم بالحقائق والتفاصيل وتتذكرها بدقة، وخاصة تلك المتعلقة بالآخرين والتفاعلات معهم. ولكي تقوم بعملك على أفضل حال، فأنت تحتاج إلى اتجاهات وتوقعات محددة بوضوح، كما أن لديك الحدس السليم الجيد وتميل إلى اتخاذ القرارات المدروسة والمعقولة. أنت شخص صبور وتراعي الآخرين، وتهتم باحتياجاتهم ومشاعرهم، ولكن النصيحة أن تشارك مشاعرك وآراءك الخاصة فقط مع أشخاص تعرفهم جيداً. أنت تحمي عائلتك وأصدقاءك، وتخلص لهم، تكرس حياتك لخدمتهم، وتفخر بإنجازاتهم، ولديك أخلاقيات عمل رفيعة وتحترم التزاماتك بجدية.",
                "Supportive, Reliable and patient, Imaginative and observant, Enthusiastic, Loyal, Hard-working",
                "داعمون، موثوقون وصبورون، خياليون ومراقبون، متحمسون، مخلصون، مجتهدون",
                "Humble and shy, Take things too personally, Repress their feelings, Overload themselves, Reluctant to change, Too altruistic",
                "متواضعون وخجولون، يأخذون الأمور شخصياً، يكبتون مشاعرهم، يحملون أنفسهم فوق طاقتهم، مترددون في التغيير، إيثاريون جداً"
            );
            personalityTypes.Add(isfj);
            var estj = new PersonalityType("ESTJ", "The Executive", "التنفيذي", 1);
            estj.UpdateContent(
                "The Executive", "التنفيذي",
                "Excellent administrators, unsurpassed at managing things – or people.",
                "أنت شخص ودود ومبادر ونزيه وتميل إلى وجهات النظر التقليدية في كثير من الأحيان، ومحافظ جداً، وتعبر عن آرائك بارتياح. أنت تثق في تجربتك الشخصية، وتهتم بشكل أكثر بالأمور الواقعية، والمشكلات الحالية وليس النظريات أو الاحتمالات. أنت شخص عملي، واقعي، ومنظم، وتسعى لغرس النظام والبناء التنظيمي، وتعمل بجد لتحقيق أو تجاوز التوقعات. أنت شخص مباشر وصريح، وترغب أن تبقي مشغولاً، وتحب أن ترى نتائجاً ملموسة لجهودك. أنت تتخذ القرارات السريعة والمنطقية، وتنتقل إلى المهمة التالية دون انتظار. أنت شخص مسؤول وصاحب ضمير حي، وتستمتع بالمسؤولية وبتنظيم الآخرين وبتنفيذ المشاريع.",
                "Dedicated, Strong-willed, Direct and honest, Loyal, Patient and reliable, Enjoy creating order",
                "متفانون، أقوياء الإرادة، مباشرون وصادقون، مخلصون، صبورون وموثوقون، يستمتعون بخلق النظام",
                "Inflexible and stubborn, Uncomfortable with unconventional situations, Judgmental, Too focused on social status, Difficult to relax",
                "غير مرنين وعنيدون، غير مرتاحين مع المواقف غير التقليدية، يحكمون على الآخرين، مركزون جداً على المكانة الاجتماعية، صعبو الاسترخاء"
            );
            personalityTypes.Add(estj);
            var esfj = new PersonalityType("ESFJ", "The Consul", "القنصل", 1);
            esfj.UpdateContent(
                "The Consul", "القنصل",
                "Extraordinarily caring, social and popular people, always eager to help.",
                "أنت شخص ودود وغير متحفظ، وتستمتع بمقابلة الآخرين، كما أن العلاقات مع الآخرين مهمة بالنسبة لك. أنت تهتم بمشاعر الآخرين، وتحرص على إرضاء ومساعدة الآخرين بطرق واقعية وعملية. أنت عاطفي وتراعي مشاعر الآخرين، وذو رأي حازم وتستند على قيمك الراسخة كشخص حيوي ومهتم بالكثير من الأشياء، ولديك العديد من المشاريع والأنشطة والأصدقاء. أنت تتمتع بحس سليم وثابت، وتتذكر التفاصيل جيداً. أنت شخص مجتهد، ومنظم وذو ضمير حي، وتستمتع بكونك جزءاً من فريق متعاون، كما أنك تقدر التقاليد، وتأخذ مسؤولياتك على محمل الجد، ومستعد لبذل الكثير من الطاقة في الأشياء التي تؤمن بها.",
                "Strong practical skills, Strong sense of duty, Very loyal, Sensitive and warm, Good at connecting with others",
                "مهارات عملية قوية، حس قوي بالواجب، مخلصون جداً، حساسون ودافئون، جيدون في التواصل مع الآخرين",
                "Worried about their social status, Inflexible, Reluctant to innovate or improvise, Vulnerable to criticism, Often too needy",
                "قلقون بشأن مكانتهم الاجتماعية، غير مرنين، مترددون في الابتكار أو الارتجال، عرضة للنقد، محتاجون جداً"
            );
            personalityTypes.Add(esfj);
            var istp = new PersonalityType("ISTP", "The Virtuoso", "الفنان الماهر", 1);
            istp.UpdateContent(
                "The Virtuoso", "الفنان الماهر",
                "Bold and practical experimenters, masters of all kinds of tools.",
                "أنت شخص هادئ ومستقل، وتحب أن تبقى مشغولاً بالمشاريع المهمة والمشوقة بالنسبة إليك. أنت تقدر مهاراتك وأداءك الجيدين وكذلك مهارات وأداء الآخرين. أنت شخص متحفظ وتحب الخصوصية، وليس من عادتك تبادل ردود أفعالك أو آرائك مع الآخرين، وكشخص مباشر وصادق، أنت تهتم بالمناقشات أكثر من الأفعال، إلّا إذا كنت على معرفة واسعة بموضوع المناقشة. أنت شخص متواضع وخلوق، وفضولي ومندفع أكثر من كونك مخطط ومنظم. أنت شخص ترتاح بالمعارف والقضايا المجردة والنظرية، ولكنك تفضل العمل مع الأشياء الحقيقية بدلاً من الأفكار المجردة، فأنت واقعي، وتجيد التحليل المنطقي، كما أنك قادر على فهم كيفية عمل الأشياء.",
                "Optimistic and energetic, Creative and practical, Spontaneous and rational, Know how to prioritize, Great in a crisis",
                "متفائلون ونشيطون، مبدعون وعمليون، عفويون وعقلانيون، يعرفون كيفية ترتيب الأولويات، عظماء في الأزمات",
                "Stubborn, Insensitive, Private and reserved, Easily bored, Dislike commitment, Risky behavior",
                "عنيدون، غير حساسين، خاصون ومتحفظون، يملون بسهولة، لا يحبون الالتزام، سلوك محفوف بالمخاطر"
            );
            personalityTypes.Add(istp);
            var isfp = new PersonalityType("ISFP", "The Adventurer", "المغامر", 1);
            isfp.UpdateContent(
                "The Adventurer", "المغامر",
                "Flexible and charming artists, always ready to explore new possibilities.",
                "أنت شخص لطيف، وهادئ ومتواضع، وقد تبدو للآخرين بارداً وغير عاطفي، ولكن لديك مشاعر عميقة تشاركها فقط مع الآخرين الذين تثق فيهم وتعرفهم جيداً. أنت شخص مخلص ومتفان وصبور، لا تحاول السيطرة أو فرض قيمك الخاصة على الآخرين، فأنت طيب القلب وجدير بالثقة وحساس وتحتاج إلى أن تكون علاقاتك ممتعة وخالية من التوتر. أنت غالباً ما تأخذ النقد البناء جداً على محمل شخصي وربما تشعر بخيبة أمل أو أذى. أنت حساس وواقعي ترغب في الاستمتاع بالحياة وتعيشها على أكمل وجه. أنت عفوي ومرح وتميل إلى الاستجابة للأحداث بدلاً من التخطيط للمستقبل.",
                "Charming, Sensitive to others, Imaginative, Passionate, Curious, Artistic",
                "جذابون، حساسون للآخرين، خياليون، شغوفون، فضوليون، فنيون",
                "Fiercely independent, Unpredictable, Easily stressed, Overly competitive, Fluctuating self-esteem",
                "مستقلون بشراسة، غير متوقعين، يتوترون بسهولة، تنافسيون جداً، تقدير الذات متقلب"
            );
            personalityTypes.Add(isfp);
            var estp = new PersonalityType("ESTP", "The Entrepreneur", "رجل الأعمال", 1);
            estp.UpdateContent(
                "The Entrepreneur", "رجل الأعمال",
                "Smart, energetic and very perceptive people, who truly enjoy living on the edge.",
                "أنت شخص منطقي، وصريح، وملاحظ دقيق جداً وتعيش اللحظة، وتقيّم الأفكار والأنشطة والآخرين من حولك باستمرار. أنت شخص نشيط ومندفع نحو المتعة، وتتوق إلى النشاط دائماً. أنت شخص واقعي، وفضولي وعملي، لا تتردد في التحدث بما في عقلك، وتعتقد أن الآخرين يجب أن يتحملوا المسؤولية عن أفعالهم. أنت عفوي ومرح، وتتمتع بكونك محور الاهتمام، ويمكنك عادة جعل الأمور مسلية. أنت جيد في ملاحظة وتذكر التفاصيل الدقيقة، ويمكنك التقييم والاستجابة بسرعة للتعامل مع المشكلات الفورية، لكنك أقل مهارة في حل المشكلات طويلة المدى.",
                "Bold, Rational and practical, Original, Perceptive, Direct, Sociable",
                "جريئون، عقلانيون وعمليون، أصليون، حساسون، مباشرون، اجتماعيون",
                "Insensitive, Impatient, Risk-prone, Unstructured, May miss the bigger picture, Defiant",
                "غير حساسين، غير صبورين، عرضة للمخاطر، غير منظمين، قد يفوتون الصورة الأكبر، متمردون"
            );
            personalityTypes.Add(estp);
            var esfp = new PersonalityType("ESFP", "The Entertainer", "المسلي", 1);
            esfp.UpdateContent(
                "The Entertainer", "المسلي",
                "Spontaneous, energetic and enthusiastic people – life is never boring around them.",
                "أنت شخص ودود وحنون ونشيط، ولديك دائرة كبيرة من الأصدقاء أنت شخص حيوي وتتحدث كثيراً وهادئ الطباع، كما أن حبك للحياة يجذب الآخرين إليك. أنت تسعى إلى المرح في كل ما تفعله، وتكون في أفضل حالاتك وتستمتع عند القيام بالأشياء مع الآخرين. أنت شخص واقعي، وعقلاني وعملي وتتعامل مع التفاصيل بشكل جيد ولديك ذاكرة رائعة للحقائق التي تخص الآخرين. أنت شخص عاطفي وحريص على المساعدة، وتحاول تجنب انتقاد الآخرين، وعادة لا ترغب في السيطرة عليهم. يمكنك استخدام الحس السليم لابتكار حلول للمشكلات الفورية وتقديم المساعدة العملية للآخرين.",
                "Bold, Original, Aesthetics and showcase, Practical, Observant, Excellent people skills",
                "جريئون، أصليون، جماليون وعرضيون، عمليون، مراقبون، مهارات ممتازة مع الناس",
                "Sensitive, Conflict-averse, Easily bored, Poor long-term planners, Unfocused",
                "حساسون، يتجنبون الصراع، يملون بسهولة، مخططون ضعفاء طويلو المدى، غير مركزين"
            );
            personalityTypes.Add(esfp);

            await _context.PersonalityTypes.AddRangeAsync(personalityTypes);
            _logger.LogInformation("Added personality types");
        }

        private async Task SeedQuestionsAsync()
        {
            if (await _context.Questions.AnyAsync())
            {
                _logger.LogInformation("Questions already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding 36 comprehensive MBTI assessment questions...");

            var questions = new List<Question>();

            questions.Add(new Question(
                1, PersonalityDimension.EI,
                "At a party do you:", "في الحفلة هل:", "En una fiesta, ¿tú:", "在聚会上你会:",
                "Interact with many, including strangers", "تتفاعل مع الكثيرين، بما في ذلك الغرباء", "Interactuar con muchos, incluidos extraños", "与许多人互动，包括陌生人", true,
                "Interact with a few, known to you", "تتفاعل مع القليل، المعروفين لك", "Interactuar con unos pocos, conocidos por ti", "与少数你认识的人互动", 1
            ));
            questions.Add(new Question(
                2, PersonalityDimension.EI,
                "At parties do you:", "في الحفلات هل:", "En las fiestas, ¿tú:", "在聚会上你会:",
                "Stay late, with increasing energy", "تبقى متأخراً، مع زيادة الطاقة", "Quedarte hasta tarde, con energía creciente", "待到很晚，精力越来越充沛", true,
                "Leave early with decreased energy", "تغادر مبكراً مع انخفاض الطاقة", "Irte temprano con energía disminuida", "早早离开，精力下降", 1
            ));
            questions.Add(new Question(
                3, PersonalityDimension.EI,
                "Are you more:", "هل أنت أكثر:", "¿Eres más:", "你更多是:",
                "Easy to approach", "سهل المقاربة", "Fácil de abordar", "容易接近", true,
                "Somewhat reserved", "محجوز إلى حد ما", "Algo reservado", "有些内向", 1
            ));
            questions.Add(new Question(
                4, PersonalityDimension.EI,
                "In your social groups do you:", "في مجموعاتك الاجتماعية هل:", "En tus grupos sociales, ¿tú:", "在你的社交圈中你会:",
                "Keep abreast of others' happenings", "تواكب أحداث الآخرين", "Mantenerte al día con lo que pasa a otros", "了解他人的近况", true,
                "Get behind on the news", "تتأخر في معرفة الأخبار", "Quedarte atrás en las noticias", "对消息了解滞后", 1
            ));
            questions.Add(new Question(
                5, PersonalityDimension.EI,
                "Are you more:", "هل أنت أكثر:", "¿Eres más:", "你更多是:",
                "Expressive", "تعبيرية", "Expresivo", "善于表达", true,
                "Deliberate", "متأني", "Deliberado", "深思熟虑", 1
            ));
            questions.Add(new Question(
                6, PersonalityDimension.EI,
                "Do you prefer to:", "هل تفضل أن:", "¿Prefieres:", "你更喜欢:",
                "Be around people most of the time", "تكون حول الناس معظم الوقت", "Estar rodeado de gente la mayor parte del tiempo", "大部分时间和人在一起", true,
                "Have some time to yourself", "تحصل على بعض الوقت لنفسك", "Tener algo de tiempo para ti mismo", "有一些独处的时间", 1
            ));
            questions.Add(new Question(
                7, PersonalityDimension.EI,
                "In a large group do you more often:", "في مجموعة كبيرة هل تفعل أكثر:", "En un grupo grande, ¿más a menudo:", "在大群体中你更经常:",
                "Introduce others", "تقدم الآخرين", "Presentas a otros", "介绍他人", true,
                "Get introduced", "يتم تقديمك", "Te presentan", "被介绍", 1
            ));
            questions.Add(new Question(
                8, PersonalityDimension.EI,
                "Do you feel better after:", "هل تشعر بتحسن بعد:", "¿Te sientes mejor después de:", "你在什么之后感觉更好:",
                "A lively discussion", "نقاش حيوي", "Una discusión animada", "热烈的讨论", true,
                "Quiet reflection", "تأمل هادئ", "Reflexión silenciosa", "安静的思考", 1
            ));
            questions.Add(new Question(
                9, PersonalityDimension.EI,
                "Are you usually:", "هل أنت عادة:", "¿Eres usualmente:", "你通常是:",
                "A good mixer", "شخص اجتماعي جيد", "Bueno para socializar", "善于交际", true,
                "Rather quiet and reserved", "هادئ ومحجوز إلى حد ما", "Bastante callado y reservado", "相当安静和内向", 1
            ));

            questions.Add(new Question(
                10, PersonalityDimension.SN,
                "Are you more:", "هل أنت أكثر:", "¿Eres más:", "你更多是:",
                "Realistic than speculative", "واقعية من التأملية", "Realista que especulativo", "现实主义而非投机", false,
                "Speculative than realistic", "تأملية من الواقعية", "Especulativo que realista", "投机而非现实主义", 1
            ));
            questions.Add(new Question(
                11, PersonalityDimension.SN,
                "Is it worse to:", "هل من الأسوأ أن:", "¿Es peor:", "更糟糕的是:",
                "Have your head in the clouds", "يكون رأسك في الغيوم", "Tener la cabeza en las nubes", "心不在焉", false,
                "Be in a rut", "تكون في روتين", "Estar en una rutina", "墨守成规", 1
            ));
            questions.Add(new Question(
                12, PersonalityDimension.SN,
                "Are you more impressed by:", "هل تتأثر أكثر بـ:", "¿Te impresionan más:", "你更容易被什么打动:",
                "Principles", "المبادئ", "Principios", "原则", false,
                "Emotions", "العواطف", "Emociones", "情感", 1
            ));
            questions.Add(new Question(
                13, PersonalityDimension.SN,
                "Are you more drawn toward the:", "هل تنجذب أكثر نحو:", "¿Te sientes más atraído hacia lo:", "你更倾向于:",
                "Convincing", "المقنع", "Convincente", "有说服力的", false,
                "Touching", "المؤثر", "Conmovedor", "感人的", 1
            ));
            questions.Add(new Question(
                14, PersonalityDimension.SN,
                "Do you prefer to work with:", "هل تفضل العمل مع:", "¿Prefieres trabajar con:", "你更喜欢使用:",
                "Facts", "الحقائق", "Hechos", "事实", false,
                "Ideas", "الأفكار", "Ideas", "想法", 1
            ));
            questions.Add(new Question(
                15, PersonalityDimension.SN,
                "Are you more interested in:", "هل أنت أكثر اهتماماً بـ:", "¿Te interesas más en:", "你更感兴趣于:",
                "What is actual", "ما هو فعلي", "Lo que es real", "实际的事物", false,
                "What is possible", "ما هو ممكن", "Lo que es posible", "可能的事物", 1
            ));
            questions.Add(new Question(
                16, PersonalityDimension.SN,
                "In judging others are you more swayed by:", "في الحكم على الآخرين هل تتأثر أكثر بـ:", "Al juzgar a otros, ¿te influye más:", "在评判他人时你更容易被什么影响:",
                "Laws than circumstances", "القوانين أكثر من الظروف", "Las leyes que las circunstancias", "法律而非情况", false,
                "Circumstances than laws", "الظروف أكثر من القوانين", "Las circunstancias que las leyes", "情况而非法律", 1
            ));
            questions.Add(new Question(
                17, PersonalityDimension.SN,
                "Do you prefer the:", "هل تفضل:", "¿Prefieres lo:", "你更喜欢:",
                "Definite", "المحدد", "Definido", "确定的", false,
                "Open-ended", "المفتوح", "Abierto", "开放式的", 1
            ));
            questions.Add(new Question(
                18, PersonalityDimension.SN,
                "Does new and non-routine interaction with others:", "هل التفاعل الجديد وغير الروتيني مع الآخرين:", "¿La interacción nueva y no rutinaria con otros:", "与他人的新颖和非常规互动:",
                "Stimulate and energize you", "يحفزك ويمنحك الطاقة", "Te estimula y energiza", "刺激并给你活力", false,
                "Tax your reserves", "يستنزف احتياطياتك", "Agota tus reservas", "消耗你的储备", 1
            ));

            questions.Add(new Question(
                19, PersonalityDimension.TF,
                "Are you more impressed by:", "هل تتأثر أكثر بـ:", "¿Te impresionan más:", "你更容易被什么打动:",
                "Principles", "المبادئ", "Principios", "原则", true,
                "Emotions", "العواطف", "Emociones", "情感", 1
            ));
            questions.Add(new Question(
                20, PersonalityDimension.TF,
                "Are you more drawn toward the:", "هل تنجذب أكثر نحو:", "¿Te sientes más atraído hacia lo:", "你更倾向于:",
                "Convincing", "المقنع", "Convincente", "有说服力的", true,
                "Touching", "المؤثر", "Conmovedor", "感人的", 1
            ));
            questions.Add(new Question(
                21, PersonalityDimension.TF,
                "Which is more admirable:", "أيهما أكثر إعجاباً:", "¿Cuál es más admirable:", "哪个更令人钦佩:",
                "The ability to organize and be methodical", "القدرة على التنظيم والمنهجية", "La capacidad de organizar y ser metódico", "组织和有条理的能力", true,
                "The ability to adapt and make do", "القدرة على التكيف والتدبير", "La capacidad de adaptarse y arreglárselas", "适应和应对的能力", 1
            ));
            questions.Add(new Question(
                22, PersonalityDimension.TF,
                "Do you put more value on:", "هل تضع قيمة أكبر على:", "¿Valoras más:", "你更重视:",
                "Infinite justice", "العدالة اللانهائية", "Justicia infinita", "无限的正义", true,
                "Infinite mercy", "الرحمة اللانهائية", "Misericordia infinita", "无限的仁慈", 1
            ));
            questions.Add(new Question(
                23, PersonalityDimension.TF,
                "Do you more often let:", "هل تدع أكثر:", "¿Más a menudo dejas que:", "你更经常让:",
                "Your head rule your heart", "عقلك يحكم قلبك", "Tu cabeza gobierne tu corazón", "理智支配情感", true,
                "Your heart rule your head", "قلبك يحكم عقلك", "Tu corazón gobierne tu cabeza", "情感支配理智", 1
            ));
            questions.Add(new Question(
                24, PersonalityDimension.TF,
                "Are you more:", "هل أنت أكثر:", "¿Eres más:", "你更多是:",
                "Fair-minded", "عادل الفكر", "Justo", "公正的", true,
                "Sympathetic", "متعاطف", "Simpático", "同情的", 1
            ));
            questions.Add(new Question(
                25, PersonalityDimension.TF,
                "Is it worse to be:", "هل من الأسوأ أن تكون:", "¿Es peor ser:", "更糟糕的是:",
                "Unjust", "غير عادل", "Injusto", "不公正", true,
                "Merciless", "بلا رحمة", "Sin piedad", "无情", 1
            ));
            questions.Add(new Question(
                26, PersonalityDimension.TF,
                "Should one usually let events occur:", "هل يجب عادة ترك الأحداث تحدث:", "¿Debería uno usualmente dejar que los eventos ocurran:", "通常应该让事件:",
                "By careful selection and choice", "بالاختيار والانتقاء الدقيق", "Por selección y elección cuidadosa", "通过仔细选择和抉择发生", true,
                "Randomly and by chance", "عشوائياً وبالصدفة", "Al azar y por casualidad", "随机和偶然地发生", 1
            ));
            questions.Add(new Question(
                27, PersonalityDimension.TF,
                "Do you feel it is a greater error:", "هل تشعر أنه خطأ أكبر:", "¿Sientes que es un error mayor:", "你觉得更大的错误是:",
                "To be too passionate", "أن تكون شغوفاً جداً", "Ser demasiado apasionado", "过于热情", true,
                "To be too objective", "أن تكون موضوعياً جداً", "Ser demasiado objetivo", "过于客观", 1
            ));

            questions.Add(new Question(
                28, PersonalityDimension.JP,
                "Do you prefer to work:", "هل تفضل العمل:", "¿Prefieres trabajar:", "你更喜欢工作:",
                "To deadlines", "وفقاً للمواعيد النهائية", "Con fechas límite", "按截止日期", true,
                "Just whenever", "في أي وقت", "Cuando sea", "随时", 1
            ));
            questions.Add(new Question(
                29, PersonalityDimension.JP,
                "Do you tend to choose:", "هل تميل إلى اختيار:", "¿Tiendes a elegir:", "你倾向于选择:",
                "Rather carefully", "بعناية إلى حد ما", "Bastante cuidadosamente", "相当仔细地", true,
                "Somewhat impulsively", "بشكل مندفع إلى حد ما", "Algo impulsivamente", "有些冲动地", 1
            ));
            questions.Add(new Question(
                30, PersonalityDimension.JP,
                "Do you tend to be more:", "هل تميل إلى أن تكون أكثر:", "¿Tiendes a ser más:", "你倾向于更:",
                "Deliberate than spontaneous", "متأني من عفوي", "Deliberado que espontáneo", "深思熟虑而非自发", true,
                "Spontaneous than deliberate", "عفوي من متأني", "Espontáneo que deliberado", "自发而非深思熟虑", 1
            ));
            questions.Add(new Question(
                31, PersonalityDimension.JP,
                "Are you more comfortable with:", "هل أنت أكثر راحة مع:", "¿Te sientes más cómodo con:", "你对什么更感到舒适:",
                "Scheduled events", "الأحداث المجدولة", "Eventos programados", "预定的事件", true,
                "Unscheduled events", "الأحداث غير المجدولة", "Eventos no programados", "未预定的事件", 1
            ));
            questions.Add(new Question(
                32, PersonalityDimension.JP,
                "Do you more often prefer the:", "هل تفضل أكثر:", "¿Más a menudo prefieres lo:", "你更经常喜欢:",
                "Final and unalterable statement", "البيان النهائي وغير القابل للتغيير", "Declaración final e inalterable", "最终和不可改变的陈述", true,
                "Tentative and preliminary statement", "البيان المؤقت والأولي", "Declaración tentativa y preliminar", "试探性和初步的陈述", 1
            ));
            questions.Add(new Question(
                33, PersonalityDimension.JP,
                "Are you more comfortable with:", "هل أنت أكثر راحة مع:", "¿Te sientes más cómodo con:", "你对什么更感到舒适:",
                "A finished task", "مهمة منجزة", "Una tarea terminada", "完成的任务", true,
                "An ongoing task", "مهمة مستمرة", "Una tarea en curso", "进行中的任务", 1
            ));
            questions.Add(new Question(
                34, PersonalityDimension.JP,
                "Do you prefer:", "هل تفضل:", "¿Prefieres:", "你更喜欢:",
                "Planned events", "الأحداث المخططة", "Eventos planificados", "计划好的事件", true,
                "Unplanned events", "الأحداث غير المخططة", "Eventos no planificados", "未计划的事件", 1
            ));
            questions.Add(new Question(
                35, PersonalityDimension.JP,
                "Do you tend to have:", "هل تميل إلى أن يكون لديك:", "¿Tiendes a tener:", "你倾向于拥有:",
                "Broad friendships with many different people", "صداقات واسعة مع أشخاص مختلفين كثيرين", "Amistades amplias con muchas personas diferentes", "与许多不同的人建立广泛的友谊", true,
                "Deep friendship with very few people", "صداقة عميقة مع عدد قليل جداً من الناس", "Amistad profunda con muy pocas personas", "与极少数人建立深厚的友谊", 1
            ));
            questions.Add(new Question(
                36, PersonalityDimension.JP,
                "Do you go more by:", "هل تسير أكثر حسب:", "¿Te guías más por:", "你更多地依据:",
                "Facts", "الحقائق", "Hechos", "事实", true,
                "Principles", "المبادئ", "Principios", "原则", 1
            ));

            await _context.Questions.AddRangeAsync(questions);
            _logger.LogInformation("Added comprehensive MBTI assessment questions");
        }

        private async Task SeedCareerClustersAsync()
        {
            if (await _context.CareerClusters.AnyAsync())
            {
                _logger.LogInformation("Career clusters already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding career clusters...");

            var careerClusters = new List<CareerCluster>();

            var stemCluster = new CareerCluster(
                "Science, Technology, Engineering & Mathematics",
                "العلوم والتكنولوجيا والهندسة والرياضيات",
                1
            );
            stemCluster.Update(
                "Science, Technology, Engineering & Mathematics",
                "العلوم والتكنولوجيا والهندسة والرياضيات",
                "Planning, managing and providing scientific research and professional and technical services.",
                "التخطيط وإدارة وتقديم البحث العلمي والخدمات المهنية والتقنية."
            );
            careerClusters.Add(stemCluster);
            var healthCluster = new CareerCluster(
                "Health Science",
                "علوم الصحة",
                1
            );
            healthCluster.Update(
                "Health Science",
                "علوم الصحة",
                "Planning, managing and providing therapeutic services, diagnostic services, health informatics, support services, and biotechnology research and development.",
                "التخطيط وإدارة وتقديم الخدمات العلاجية والتشخيصية ومعلوماتية الصحة وخدمات الدعم وبحث وتطوير التكنولوجيا الحيوية."
            );
            careerClusters.Add(healthCluster);
            var businessCluster = new CareerCluster(
                "Business Management & Administration",
                "إدارة الأعمال والإدارة",
                1
            );
            businessCluster.Update(
                "Business Management & Administration",
                "إدارة الأعمال والإدارة",
                "Planning, organizing, directing and evaluating business functions essential to efficient and productive business operations.",
                "التخطيط والتنظيم والتوجيه والتقييم لوظائف الأعمال الأساسية لعمليات الأعمال الفعالة والمنتجة."
            );
            careerClusters.Add(businessCluster);
            var educationCluster = new CareerCluster(
                "Education & Training",
                "التعليم والتدريب",
                1
            );
            educationCluster.Update(
                "Education & Training",
                "التعليم والتدريب",
                "Planning, managing and providing education and training services, and related learning support services.",
                "التخطيط وإدارة وتقديم خدمات التعليم والتدريب وخدمات دعم التعلم ذات الصلة."
            );
            careerClusters.Add(educationCluster);

            await _context.CareerClusters.AddRangeAsync(careerClusters);
            await _context.SaveChangesAsync(); // Save immediately to ensure they're available for careers seeding
            _logger.LogInformation("Added career clusters");
        }

        private async Task SeedCareersAsync()
        {
            if (await _context.Careers.AnyAsync())
            {
                _logger.LogInformation("Careers already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding careers...");

            await _context.SaveChangesAsync(); // Ensure career clusters are saved before querying
            
            var stemCluster = await _context.CareerClusters.FirstAsync(c => c.NameEn == "Science, Technology, Engineering & Mathematics");
            var healthCluster = await _context.CareerClusters.FirstAsync(c => c.NameEn == "Health Science");
            var businessCluster = await _context.CareerClusters.FirstAsync(c => c.NameEn == "Business Management & Administration");
            var educationCluster = await _context.CareerClusters.FirstAsync(c => c.NameEn == "Education & Training");

            var careers = new List<Career>();

            var softwareEngineer = new Career("Software Engineer", "مهندس برمجيات", stemCluster.Id, 1);
            softwareEngineer.Update(
                "Software Engineer", "مهندس برمجيات",
                "Design, develop, and maintain software applications and systems.",
                "تصميم وتطوير وصيانة تطبيقات وأنظمة البرمجيات.",
                "2511"
            );
            careers.Add(softwareEngineer);
            var dataScientist = new Career("Data Scientist", "عالم بيانات", stemCluster.Id, 1);
            dataScientist.Update(
                "Data Scientist", "عالم بيانات",
                "Analyze complex data to help organizations make informed decisions.",
                "تحليل البيانات المعقدة لمساعدة المنظمات على اتخاذ قرارات مدروسة.",
                "2120"
            );
            careers.Add(dataScientist);
            var registeredNurse = new Career("Registered Nurse", "ممرض مسجل", healthCluster.Id, 1);
            registeredNurse.Update(
                "Registered Nurse", "ممرض مسجل",
                "Provide patient care and support in healthcare settings.",
                "تقديم رعاية ودعم المرضى في البيئات الصحية.",
                "2221"
            );
            careers.Add(registeredNurse);
            var businessAnalyst = new Career("Business Analyst", "محلل أعمال", businessCluster.Id, 1);
            businessAnalyst.Update(
                "Business Analyst", "محلل أعمال",
                "Analyze business processes and recommend improvements.",
                "تحليل العمليات التجارية والتوصية بالتحسينات.",
                "2421"
            );
            careers.Add(businessAnalyst);
            var teacher = new Career("Teacher", "معلم", educationCluster.Id, 1);
            teacher.Update(
                "Teacher", "معلم",
                "Educate and inspire students in various subjects and grade levels.",
                "تعليم وإلهام الطلاب في مواد ومستويات دراسية مختلفة.",
                "2341"
            );
            careers.Add(teacher);

            await _context.Careers.AddRangeAsync(careers);
            _logger.LogInformation("Added careers");
        }

        private async Task SeedPathwaysAsync()
        {
            if (await _context.Pathways.AnyAsync())
            {
                _logger.LogInformation("Pathways already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding pathways...");

            var pathways = new List<Pathway>();

            var moeScientific = new Pathway(
                "Scientific Track", 
                "المسار العلمي", 
                PathwaySource.MOE, 
                1
            );
            moeScientific.Update(
                "Scientific Track", "المسار العلمي",
                PathwaySource.MOE,
                "Focuses on mathematics, physics, chemistry, and biology for students interested in scientific careers.",
                "يركز على الرياضيات والفيزياء والكيمياء والأحياء للطلاب المهتمين بالمهن العلمية."
            );
            pathways.Add(moeScientific);

            var moeHumanities = new Pathway(
                "Humanities Track", 
                "المسار الإنساني", 
                PathwaySource.MOE, 
                1
            );
            moeHumanities.Update(
                "Humanities Track", "المسار الإنساني",
                PathwaySource.MOE,
                "Emphasizes literature, history, geography, and social sciences for students interested in humanities careers.",
                "يؤكد على الأدب والتاريخ والجغرافيا والعلوم الاجتماعية للطلاب المهتمين بمهن العلوم الإنسانية."
            );
            pathways.Add(moeHumanities);

            var moeBusiness = new Pathway(
                "Business and Administration Track", 
                "مسار الأعمال والإدارة", 
                PathwaySource.MOE, 
                1
            );
            moeBusiness.Update(
                "Business and Administration Track", "مسار الأعمال والإدارة",
                PathwaySource.MOE,
                "Covers business principles, economics, accounting, and management for future business leaders.",
                "يغطي مبادئ الأعمال والاقتصاد والمحاسبة والإدارة لقادة الأعمال المستقبليين."
            );
            pathways.Add(moeBusiness);

            var moeHealth = new Pathway(
                "Health Sciences Track", 
                "مسار العلوم الصحية", 
                PathwaySource.MOE, 
                1
            );
            moeHealth.Update(
                "Health Sciences Track", "مسار العلوم الصحية",
                PathwaySource.MOE,
                "Prepares students for healthcare careers through biology, chemistry, and health science fundamentals.",
                "يعد الطلاب لمهن الرعاية الصحية من خلال الأحياء والكيمياء وأساسيات علوم الصحة."
            );
            pathways.Add(moeHealth);

            var moeTechnology = new Pathway(
                "Technology and Engineering Track", 
                "مسار التكنولوجيا والهندسة", 
                PathwaySource.MOE, 
                1
            );
            moeTechnology.Update(
                "Technology and Engineering Track", "مسار التكنولوجيا والهندسة",
                PathwaySource.MOE,
                "Focuses on engineering principles, computer science, and technology applications.",
                "يركز على مبادئ الهندسة وعلوم الحاسوب وتطبيقات التكنولوجيا."
            );
            pathways.Add(moeTechnology);

            var mawhibaMedical = new Pathway(
                "Medical, Biological and Chemical Sciences", 
                "العلوم الطبية والبيولوجية والكيميائية", 
                PathwaySource.MAWHIBA, 
                1
            );
            mawhibaMedical.Update(
                "Medical, Biological and Chemical Sciences", "العلوم الطبية والبيولوجية والكيميائية",
                PathwaySource.MAWHIBA,
                "Advanced program for gifted students in medical sciences, biochemistry, and molecular biology.",
                "برنامج متقدم للطلاب الموهوبين في العلوم الطبية والكيمياء الحيوية والبيولوجيا الجزيئية."
            );
            pathways.Add(mawhibaMedical);

            var mawhibaPhysics = new Pathway(
                "Physics, Earth and Space Sciences", 
                "الفيزياء وعلوم الأرض والفضاء", 
                PathwaySource.MAWHIBA, 
                1
            );
            mawhibaPhysics.Update(
                "Physics, Earth and Space Sciences", "الفيزياء وعلوم الأرض والفضاء",
                PathwaySource.MAWHIBA,
                "Specialized track for gifted students in physics, astronomy, geology, and space sciences.",
                "مسار متخصص للطلاب الموهوبين في الفيزياء وعلم الفلك والجيولوجيا وعلوم الفضاء."
            );
            pathways.Add(mawhibaPhysics);

            var mawhibaEngineering = new Pathway(
                "Engineering Studies", 
                "الدراسات الهندسية", 
                PathwaySource.MAWHIBA, 
                1
            );
            mawhibaEngineering.Update(
                "Engineering Studies", "الدراسات الهندسية",
                PathwaySource.MAWHIBA,
                "Intensive engineering program for gifted students covering multiple engineering disciplines.",
                "برنامج هندسي مكثف للطلاب الموهوبين يغطي تخصصات هندسية متعددة."
            );
            pathways.Add(mawhibaEngineering);

            var mawhibaComputer = new Pathway(
                "Computer and Applied Mathematics", 
                "الحاسوب والرياضيات التطبيقية", 
                PathwaySource.MAWHIBA, 
                1
            );
            mawhibaComputer.Update(
                "Computer and Applied Mathematics", "الحاسوب والرياضيات التطبيقية",
                PathwaySource.MAWHIBA,
                "Advanced program for gifted students in computer science, algorithms, and applied mathematics.",
                "برنامج متقدم للطلاب الموهوبين في علوم الحاسوب والخوارزميات والرياضيات التطبيقية."
            );
            pathways.Add(mawhibaComputer);

            await _context.Pathways.AddRangeAsync(pathways);
            await _context.SaveChangesAsync(); // Save immediately to ensure they're available for career mappings
            _logger.LogInformation("Added pathways (MOE + Mawhiba)");
        }

        private async Task SeedCareerPathwayMappingsAsync()
        {
            if (await _context.CareerPathways.AnyAsync())
            {
                _logger.LogInformation("Career pathway mappings already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding career pathway mappings...");

            await _context.SaveChangesAsync(); // Ensure pathways and careers are saved before querying

            var pathways = await _context.Pathways.ToListAsync();
            var careers = await _context.Careers.ToListAsync();

            var careerPathwayMappings = new List<CareerPathway>();

            foreach (var career in careers)
            {
                foreach (var pathway in pathways)
                {
                    var recommendationScore = CalculateCareerPathwayScore(career.NameEn, pathway.NameEn, pathway.Source);
                    
                    if (recommendationScore > 0.3m) // Only create mappings with meaningful scores
                    {
                        var mapping = new CareerPathway(
                            career.Id,
                            pathway.Id,
                            1
                        );
                        careerPathwayMappings.Add(mapping);
                    }
                }
            }

            await _context.CareerPathways.AddRangeAsync(careerPathwayMappings);
            _logger.LogInformation("Added career pathway mappings");
        }

        private static decimal CalculateCareerPathwayScore(string careerName, string pathwayName, PathwaySource pathwaySource)
        {
            var baseScore = 0.3m;
            var score = baseScore;

            if (careerName.Contains("Software") || careerName.Contains("Data"))
            {
                if (pathwayName.Contains("Technology") || pathwayName.Contains("Computer") || pathwayName.Contains("Engineering"))
                {
                    score = pathwaySource == PathwaySource.MAWHIBA ? 0.9m : 0.8m;
                }
                else if (pathwayName.Contains("Scientific"))
                {
                    score = 0.6m;
                }
            }
            else if (careerName.Contains("Nurse") || careerName.Contains("Health"))
            {
                if (pathwayName.Contains("Health") || pathwayName.Contains("Medical") || pathwayName.Contains("Biological"))
                {
                    score = pathwaySource == PathwaySource.MAWHIBA ? 0.9m : 0.8m;
                }
                else if (pathwayName.Contains("Scientific"))
                {
                    score = 0.6m;
                }
            }
            else if (careerName.Contains("Business") || careerName.Contains("Analyst"))
            {
                if (pathwayName.Contains("Business") || pathwayName.Contains("Administration"))
                {
                    score = 0.8m;
                }
                else if (pathwayName.Contains("Applied Mathematics"))
                {
                    score = pathwaySource == PathwaySource.MAWHIBA ? 0.7m : 0.5m;
                }
            }
            else if (careerName.Contains("Teacher") || careerName.Contains("Education"))
            {
                if (pathwayName.Contains("Humanities"))
                {
                    score = 0.7m;
                }
                else if (pathwayName.Contains("Scientific") || pathwayName.Contains("Technology"))
                {
                    score = 0.6m;
                }
            }

            if (pathwaySource == PathwaySource.MAWHIBA && score > 0.5m)
            {
                score = Math.Min(1.0m, score + 0.1m);
            }

            return score;
        }

        private async Task SeedPersonalityCareerMatchesAsync()
        {
            if (await _context.PersonalityCareerMatches.AnyAsync())
            {
                _logger.LogInformation("Personality career matches already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding personality career matches...");

            var personalityTypes = await _context.PersonalityTypes.ToListAsync();
            var careers = await _context.Careers.ToListAsync();

            var matches = new List<PersonalityCareerMatch>();

            foreach (var personalityType in personalityTypes)
            {
                foreach (var career in careers)
                {
                    var matchScore = CalculateMatchScore(personalityType.Code, career.NameEn);
                    
                    var match = new PersonalityCareerMatch(
                        personalityType.Id,
                        career.Id,
                        matchScore,
                        1
                    );
                    
                    matches.Add(match);
                }
            }

            await _context.PersonalityCareerMatches.AddRangeAsync(matches);
            _logger.LogInformation("Added personality career matches");
        }

        private static decimal CalculateMatchScore(string personalityCode, string careerTitle)
        {
            var baseScore = 0.5m;
            
            switch (personalityCode)
            {
                case "INTJ":
                case "INTP":
                    if (careerTitle.Contains("Software") || careerTitle.Contains("Data"))
                        return 0.9m;
                    break;
                case "ENTJ":
                case "ESTJ":
                    if (careerTitle.Contains("Business") || careerTitle.Contains("Analyst"))
                        return 0.85m;
                    break;
                case "ENFJ":
                case "ESFJ":
                    if (careerTitle.Contains("Teacher") || careerTitle.Contains("Nurse"))
                        return 0.8m;
                    break;
                case "ISFJ":
                case "INFJ":
                    if (careerTitle.Contains("Nurse") || careerTitle.Contains("Teacher"))
                        return 0.75m;
                    break;
            }
            
            return baseScore + (decimal)(new Random().NextDouble() * 0.3);
        }

        private async Task SeedCareerClusterRatingsAsync()
        {
            if (await _context.CareerClusterRatings.AnyAsync())
            {
                _logger.LogInformation("Career cluster ratings already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding career cluster ratings...");

            var careerClusterRatings = new[]
            {
                new CareerClusterRating(1, "Not interested at all", "غير مهتم على الإطلاق", 1),
                new CareerClusterRating(2, "Slightly interested", "مهتم قليلاً", 1),
                new CareerClusterRating(3, "Moderately interested", "مهتم بشكل معتدل", 1),
                new CareerClusterRating(4, "Very interested", "مهتم جداً", 1),
                new CareerClusterRating(5, "Extremely interested", "مهتم للغاية", 1)
            };

            await _context.CareerClusterRatings.AddRangeAsync(careerClusterRatings);
            _logger.LogInformation("Seeded career cluster ratings");
        }

        private async Task SeedReportElementsAsync()
        {
            await Task.CompletedTask;
            _logger.LogInformation("Report elements seeding skipped - requires assessment session context");
        }

        private async Task SeedTieBreakerQuestionsAsync()
        {
            if (await _context.TieBreakerQuestions.AnyAsync())
            {
                _logger.LogInformation("Tie-breaker questions already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding tie-breaker questions...");

            var tieBreakerQuestions = new[]
            {
                new TieBreakerQuestion(
                    "When making important decisions, do you prefer to:",
                    "عند اتخاذ قرارات مهمة، هل تفضل أن:",
                    "Rely on logical analysis and objective criteria",
                    "تعتمد على التحليل المنطقي والمعايير الموضوعية",
                    "Consider personal values and how it affects people",
                    "تأخذ في الاعتبار القيم الشخصية وكيف يؤثر ذلك على الناس",
                    PersonalityDimension.TF,
                    false,
                    1,
                    1
                ),
                new TieBreakerQuestion(
                    "In social situations, do you typically:",
                    "في المواقف الاجتماعية، هل تفعل عادة:",
                    "Feel energized by interacting with many people",
                    "تشعر بالنشاط من التفاعل مع الكثير من الناس",
                    "Prefer deeper conversations with a few close friends",
                    "تفضل المحادثات العميقة مع عدد قليل من الأصدقاء المقربين",
                    PersonalityDimension.EI,
                    true,
                    2,
                    1
                ),
                new TieBreakerQuestion(
                    "When processing information, do you tend to:",
                    "عند معالجة المعلومات، هل تميل إلى:",
                    "Focus on concrete facts and practical details",
                    "التركيز على الحقائق الملموسة والتفاصيل العملية",
                    "Look for patterns, possibilities, and future implications",
                    "البحث عن الأنماط والاحتمالات والآثار المستقبلية",
                    PersonalityDimension.SN,
                    true,
                    3,
                    1
                ),
                new TieBreakerQuestion(
                    "In your approach to life, do you prefer to:",
                    "في نهجك في الحياة، هل تفضل أن:",
                    "Have things planned and organized in advance",
                    "تخطط وتنظم الأشياء مسبقاً",
                    "Keep options open and adapt as situations arise",
                    "تبقي الخيارات مفتوحة وتتكيف مع المواقف عند حدوثها",
                    PersonalityDimension.JP,
                    true,
                    4,
                    1
                )
            };

            await _context.TieBreakerQuestions.AddRangeAsync(tieBreakerQuestions);
            _logger.LogInformation("Seeded tie-breaker questions");
        }
    }
}
