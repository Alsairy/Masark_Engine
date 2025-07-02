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
                    _logger.LogInformation("Created role: {RoleName}", roleName);
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
                _logger.LogError("Failed to create Identity admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
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
                "مفكرون خياليون واستراتيجيون، لديهم خطة لكل شيء.",
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
                "مخترعون مبتكرون لديهم عطش لا ينضب للمعرفة.",
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
                "قادة جريئون وخياليون وأقوياء الإرادة، يجدون دائماً طريقة أو يصنعونها.",
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
                "مفكرون أذكياء وفضوليون لا يستطيعون مقاومة التحدي الفكري.",
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
                "هادئون وغامضون، لكنهم مُلهمون جداً ومثاليون لا يكلون.",
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
                "أشخاص شاعريون ولطفاء وإيثاريون، متحمسون دائماً لمساعدة قضية جيدة.",
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
                "قادة جذابون وملهمون، قادرون على سحر مستمعيهم.",
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
                "أرواح حرة متحمسة ومبدعة واجتماعية، يمكنها دائماً إيجاد سبب للابتسام.",
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
                "عمليون ومهتمون بالحقائق، لا يمكن الشك في موثوقيتهم.",
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
                "دافئو القلب ومتفانون، مستعدون دائماً لحماية أحبائهم.",
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
                "إداريون ممتازون، لا يُضاهون في إدارة الأشياء أو الأشخاص.",
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
                "أشخاص مهتمون واجتماعيون ومحبوبون بشكل استثنائي، متحمسون دائماً للمساعدة.",
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
                "مجربون جريئون وعمليون، أسياد جميع أنواع الأدوات.",
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
                "فنانون مرنون وجذابون، مستعدون دائماً لاستكشاف إمكانيات جديدة.",
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
                "أشخاص أذكياء ونشيطون وحساسون جداً، يستمتعون حقاً بالعيش على الحافة.",
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
                "أشخاص عفويون ونشيطون ومتحمسون - الحياة لا تكون مملة أبداً من حولهم.",
                "Bold, Original, Aesthetics and showcase, Practical, Observant, Excellent people skills",
                "جريئون، أصليون، جماليون وعرضيون، عمليون، مراقبون، مهارات ممتازة مع الناس",
                "Sensitive, Conflict-averse, Easily bored, Poor long-term planners, Unfocused",
                "حساسون، يتجنبون الصراع، يملون بسهولة، مخططون ضعفاء طويلو المدى، غير مركزين"
            );
            personalityTypes.Add(esfp);

            await _context.PersonalityTypes.AddRangeAsync(personalityTypes);
            _logger.LogInformation($"Added {personalityTypes.Count} personality types");
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
            _logger.LogInformation($"Added {questions.Count} comprehensive MBTI assessment questions");
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
            _logger.LogInformation($"Added {careerClusters.Count} career clusters");
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
            _logger.LogInformation($"Added {careers.Count} careers");
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
            _logger.LogInformation($"Added {matches.Count} personality career matches");
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
            _logger.LogInformation("Seeded {Count} career cluster ratings", careerClusterRatings.Length);
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
            _logger.LogInformation("Seeded {Count} tie-breaker questions", tieBreakerQuestions.Length);
        }
    }
}
