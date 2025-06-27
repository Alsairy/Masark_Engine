using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Masark.Infrastructure.Identity;
using Masark.Domain.Entities;
using Masark.Domain.Enums;

namespace Masark.Infrastructure.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                await SeedPersonalityTypesAsync();
                await SeedQuestionsAsync();
                await SeedCareerClustersAsync();
                await SeedCareersAsync();
                await SeedPersonalityCareerMatchesAsync();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                throw;
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

            _logger.LogInformation("Seeding assessment questions...");

            var questions = new List<Question>();

            var question1 = new Question(
                1, PersonalityDimension.EI,
                "At a party do you:", "في الحفلة هل:",
                "Interact with many, including strangers", "تتفاعل مع الكثيرين، بما في ذلك الغرباء", true,
                "Interact with a few, known to you", "تتفاعل مع القليل، المعروفين لك", 1
            );
            questions.Add(question1);
            var question2 = new Question(
                2, PersonalityDimension.SN,
                "Are you more:", "هل أنت أكثر:",
                "Realistic than speculative", "واقعية من التأملية", false,
                "Speculative than realistic", "تأملية من الواقعية", 1
            );
            questions.Add(question2);
            var question3 = new Question(
                3, PersonalityDimension.SN,
                "Is it worse to:", "هل من الأسوأ أن:",
                "Have your head in the clouds", "يكون رأسك في الغيوم", false,
                "Be in a rut", "تكون في روتين", 1
            );
            questions.Add(question3);
            var question4 = new Question(
                4, PersonalityDimension.TF,
                "Are you more impressed by:", "هل تتأثر أكثر بـ:",
                "Principles", "المبادئ", true,
                "Emotions", "العواطف", 1
            );
            questions.Add(question4);
            var question5 = new Question(
                5, PersonalityDimension.TF,
                "Are you more drawn toward the:", "هل تنجذب أكثر نحو:",
                "Convincing", "المقنع", true,
                "Touching", "المؤثر", 1
            );
            questions.Add(question5);
            var question6 = new Question(
                6, PersonalityDimension.JP,
                "Do you prefer to work:", "هل تفضل العمل:",
                "To deadlines", "وفقاً للمواعيد النهائية", true,
                "Just whenever", "في أي وقت", 1
            );
            questions.Add(question6);
            var question7 = new Question(
                7, PersonalityDimension.JP,
                "Do you tend to choose:", "هل تميل إلى اختيار:",
                "Rather carefully", "بعناية إلى حد ما", true,
                "Somewhat impulsively", "بشكل مندفع إلى حد ما", 1
            );
            questions.Add(question7);
            var question8 = new Question(
                8, PersonalityDimension.EI,
                "At parties do you:", "في الحفلات هل:",
                "Stay late, with increasing energy", "تبقى متأخراً، مع زيادة الطاقة", true,
                "Leave early with decreased energy", "تغادر مبكراً مع انخفاض الطاقة", 1
            );
            questions.Add(question8);

            await _context.Questions.AddRangeAsync(questions);
            _logger.LogInformation($"Added {questions.Count} assessment questions");
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
    }
}
