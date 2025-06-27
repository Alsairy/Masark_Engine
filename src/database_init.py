"""
Database initialization script for Masark Mawhiba Personality-Career Matching Engine
This script populates the database with initial data including:
- 16 MBTI personality types
- 9 career clusters
- MOE and Mawhiba pathways
- Sample assessment questions
- Default system configurations
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.models.masark_models import (
    db, PersonalityType, CareerCluster, Pathway, Question, SystemConfiguration,
    PersonalityDimension, PathwaySource, DeploymentMode, AdminUser
)
from werkzeug.security import generate_password_hash
from flask import Flask
import json

def initialize_personality_types():
    """Initialize the 16 MBTI personality types with descriptions"""
    personality_types = [
        {
            'code': 'INTJ',
            'name_en': 'The Strategist',
            'name_ar': 'الاستراتيجي',
            'description_en': 'Innovative, independent, and strategic. Natural leaders who are driven to turn theories into realities.',
            'description_ar': 'مبتكر ومستقل واستراتيجي. قادة طبيعيون مدفوعون لتحويل النظريات إلى حقائق.',
            'strengths_en': 'Strategic thinking, independence, confidence, determination, hard-working',
            'strengths_ar': 'التفكير الاستراتيجي، الاستقلالية، الثقة، التصميم، العمل الجاد',
            'challenges_en': 'May appear aloof, can be overly critical, struggles with emotions, impatient with inefficiency',
            'challenges_ar': 'قد يبدو منعزلاً، يمكن أن يكون نقدياً بشكل مفرط، يواجه صعوبة مع العواطف، غير صبور مع عدم الكفاءة'
        },
        {
            'code': 'INTP',
            'name_en': 'The Thinker',
            'name_ar': 'المفكر',
            'description_en': 'Quiet, analytical, and insightful. Driven by curiosity and a desire to understand how things work.',
            'description_ar': 'هادئ وتحليلي وبصير. مدفوع بالفضول ورغبة في فهم كيف تعمل الأشياء.',
            'strengths_en': 'Analytical thinking, creativity, objectivity, intellectual curiosity, independent',
            'strengths_ar': 'التفكير التحليلي، الإبداع، الموضوعية، الفضول الفكري، الاستقلالية',
            'challenges_en': 'Procrastination, difficulty with deadlines, may seem insensitive, struggles with routine tasks',
            'challenges_ar': 'التسويف، صعوبة مع المواعيد النهائية، قد يبدو غير حساس، يواجه صعوبة مع المهام الروتينية'
        },
        {
            'code': 'ENTJ',
            'name_en': 'The Commander',
            'name_ar': 'القائد',
            'description_en': 'Bold, imaginative, and strong-willed leaders who find a way or make one.',
            'description_ar': 'قادة جريئون ومتخيلون وأقوياء الإرادة يجدون طريقاً أو يصنعون واحداً.',
            'strengths_en': 'Natural leadership, strategic thinking, efficient, confident, charismatic',
            'strengths_ar': 'القيادة الطبيعية، التفكير الاستراتيجي، الكفاءة، الثقة، الكاريزما',
            'challenges_en': 'Impatient, can be ruthless, difficulty expressing emotions, may seem arrogant',
            'challenges_ar': 'غير صبور، يمكن أن يكون قاسياً، صعوبة في التعبير عن المشاعر، قد يبدو متغطرساً'
        },
        {
            'code': 'ENTP',
            'name_en': 'The Innovator',
            'name_ar': 'المبتكر',
            'description_en': 'Smart, curious, and able to grasp complex concepts and ideas quickly.',
            'description_ar': 'ذكي وفضولي وقادر على استيعاب المفاهيم والأفكار المعقدة بسرعة.',
            'strengths_en': 'Innovation, enthusiasm, versatility, excellent communication, quick thinking',
            'strengths_ar': 'الابتكار، الحماس، التنوع، التواصل الممتاز، التفكير السريع',
            'challenges_en': 'Difficulty focusing, procrastination, may neglect details, can be argumentative',
            'challenges_ar': 'صعوبة في التركيز، التسويف، قد يهمل التفاصيل، يمكن أن يكون جدلياً'
        },
        {
            'code': 'INFJ',
            'name_en': 'The Advocate',
            'name_ar': 'المدافع',
            'description_en': 'Creative, insightful, and principled. Motivated by deeply held beliefs and desire to help others.',
            'description_ar': 'مبدع وبصير ومبدئي. مدفوع بمعتقدات راسخة ورغبة في مساعدة الآخرين.',
            'strengths_en': 'Empathy, insight, determination, passion, altruism',
            'strengths_ar': 'التعاطف، البصيرة، التصميم، الشغف، الإيثار',
            'challenges_en': 'Perfectionism, sensitivity to criticism, may burn out, difficulty with conflict',
            'challenges_ar': 'الكمالية، الحساسية للنقد، قد يصاب بالإرهاق، صعوبة مع الصراع'
        },
        {
            'code': 'INFP',
            'name_en': 'The Mediator',
            'name_ar': 'الوسيط',
            'description_en': 'Loyal, creative, and always looking for the good in people and events.',
            'description_ar': 'مخلص ومبدع ويبحث دائماً عن الخير في الناس والأحداث.',
            'strengths_en': 'Creativity, empathy, open-mindedness, flexibility, passion',
            'strengths_ar': 'الإبداع، التعاطف، الانفتاح الذهني، المرونة، الشغف',
            'challenges_en': 'Overly idealistic, difficulty with criticism, may neglect details, can be impractical',
            'challenges_ar': 'مثالي بشكل مفرط، صعوبة مع النقد، قد يهمل التفاصيل، يمكن أن يكون غير عملي'
        },
        {
            'code': 'ENFJ',
            'name_en': 'The Protagonist',
            'name_ar': 'البطل',
            'description_en': 'Charismatic, inspiring leaders who are able to mesmerize listeners.',
            'description_ar': 'قادة كاريزماتيون وملهمون قادرون على سحر المستمعين.',
            'strengths_en': 'Leadership, empathy, communication, charisma, altruism',
            'strengths_ar': 'القيادة، التعاطف، التواصل، الكاريزما، الإيثار',
            'challenges_en': 'Overly idealistic, too selfless, sensitive to criticism, difficulty making tough decisions',
            'challenges_ar': 'مثالي بشكل مفرط، غير أناني جداً، حساس للنقد، صعوبة في اتخاذ قرارات صعبة'
        },
        {
            'code': 'ENFP',
            'name_en': 'The Campaigner',
            'name_ar': 'المناضل',
            'description_en': 'Enthusiastic, creative, and sociable free spirits who can always find a reason to smile.',
            'description_ar': 'أرواح حرة متحمسة ومبدعة واجتماعية يمكنها دائماً العثور على سبب للابتسام.',
            'strengths_en': 'Enthusiasm, creativity, sociability, optimism, excellent communication',
            'strengths_ar': 'الحماس، الإبداع، الاجتماعية، التفاؤل، التواصل الممتاز',
            'challenges_en': 'Difficulty focusing, overthinking, disorganized, overly emotional, stress-prone',
            'challenges_ar': 'صعوبة في التركيز، الإفراط في التفكير، غير منظم، عاطفي بشكل مفرط، عرضة للتوتر'
        },
        {
            'code': 'ISTJ',
            'name_en': 'The Logistician',
            'name_ar': 'اللوجستي',
            'description_en': 'Practical, fact-minded, and reliable. They can always be counted on to get the job done.',
            'description_ar': 'عملي ومهتم بالحقائق وموثوق. يمكن الاعتماد عليهم دائماً لإنجاز العمل.',
            'strengths_en': 'Reliability, practicality, organization, loyalty, hard-working',
            'strengths_ar': 'الموثوقية، العملية، التنظيم، الولاء، العمل الجاد',
            'challenges_en': 'Resistance to change, difficulty expressing emotions, may be too rigid, struggles with abstract concepts',
            'challenges_ar': 'مقاومة التغيير، صعوبة في التعبير عن المشاعر، قد يكون جامداً جداً، يواجه صعوبة مع المفاهيم المجردة'
        },
        {
            'code': 'ISFJ',
            'name_en': 'The Protector',
            'name_ar': 'الحامي',
            'description_en': 'Warm, considerate, and responsible. They have a strong desire to serve and protect others.',
            'description_ar': 'دافئ ومتفهم ومسؤول. لديهم رغبة قوية في خدمة وحماية الآخرين.',
            'strengths_en': 'Supportiveness, reliability, patience, imagination, loyalty',
            'strengths_ar': 'الدعم، الموثوقية، الصبر، الخيال، الولاء',
            'challenges_en': 'Too selfless, difficulty saying no, sensitive to criticism, reluctant to change',
            'challenges_ar': 'غير أناني جداً، صعوبة في قول لا، حساس للنقد، مقاوم للتغيير'
        },
        {
            'code': 'ESTJ',
            'name_en': 'The Executive',
            'name_ar': 'التنفيذي',
            'description_en': 'Excellent administrators, unsurpassed at managing things or people.',
            'description_ar': 'إداريون ممتازون، لا يضاهون في إدارة الأشياء أو الناس.',
            'strengths_en': 'Leadership, organization, reliability, dedication, strong-willed',
            'strengths_ar': 'القيادة، التنظيم، الموثوقية، التفاني، قوة الإرادة',
            'challenges_en': 'Inflexible, difficulty expressing emotions, judgmental, impatient with inefficiency',
            'challenges_ar': 'غير مرن، صعوبة في التعبير عن المشاعر، يصدر أحكاماً، غير صبور مع عدم الكفاءة'
        },
        {
            'code': 'ESFJ',
            'name_en': 'The Consul',
            'name_ar': 'القنصل',
            'description_en': 'Extraordinarily caring, social, and popular people, always eager to help.',
            'description_ar': 'أشخاص مهتمون واجتماعيون ومحبوبون بشكل استثنائي، دائماً حريصون على المساعدة.',
            'strengths_en': 'Supportiveness, loyalty, sensitivity, warmth, good practical skills',
            'strengths_ar': 'الدعم، الولاء، الحساسية، الدفء، المهارات العملية الجيدة',
            'challenges_en': 'Worried about social status, inflexible, reluctant to innovate, vulnerable to criticism',
            'challenges_ar': 'قلق بشأن المكانة الاجتماعية، غير مرن، مقاوم للابتكار، عرضة للنقد'
        },
        {
            'code': 'ISTP',
            'name_en': 'The Virtuoso',
            'name_ar': 'البارع',
            'description_en': 'Bold, practical experimenters, masters of all kinds of tools.',
            'description_ar': 'مجربون جريئون وعمليون، أسياد جميع أنواع الأدوات.',
            'strengths_en': 'Practical, flexible, crisis management, relaxed, optimistic',
            'strengths_ar': 'عملي، مرن، إدارة الأزمات، مسترخي، متفائل',
            'challenges_en': 'Stubborn, insensitive, private, easily bored, dislikes commitment',
            'challenges_ar': 'عنيد، غير حساس، خاص، يمل بسهولة، لا يحب الالتزام'
        },
        {
            'code': 'ISFP',
            'name_en': 'The Adventurer',
            'name_ar': 'المغامر',
            'description_en': 'Flexible, charming artists who are always ready to explore new possibilities.',
            'description_ar': 'فنانون مرنون وساحرون دائماً مستعدون لاستكشاف إمكانيات جديدة.',
            'strengths_en': 'Creativity, passion, curiosity, artistic skills, flexibility',
            'strengths_ar': 'الإبداع، الشغف، الفضول، المهارات الفنية، المرونة',
            'challenges_en': 'Overly competitive, difficulty with long-term planning, stress-prone, independent to a fault',
            'challenges_ar': 'تنافسي بشكل مفرط، صعوبة في التخطيط طويل المدى، عرضة للتوتر، مستقل إلى حد الخطأ'
        },
        {
            'code': 'ESTP',
            'name_en': 'The Entrepreneur',
            'name_ar': 'رجل الأعمال',
            'description_en': 'Smart, energetic, and perceptive people who truly enjoy living on the edge.',
            'description_ar': 'أشخاص أذكياء ونشطون وحساسون يستمتعون حقاً بالعيش على الحافة.',
            'strengths_en': 'Bold, rational, practical, original, perceptive',
            'strengths_ar': 'جريء، عقلاني، عملي، أصيل، حساس',
            'challenges_en': 'Impatient, risk-prone, unstructured, may miss the bigger picture, defiant',
            'challenges_ar': 'غير صبور، عرضة للمخاطر، غير منظم، قد يفوت الصورة الأكبر، متمرد'
        },
        {
            'code': 'ESFP',
            'name_en': 'The Entertainer',
            'name_ar': 'المسلي',
            'description_en': 'Spontaneous, energetic, and enthusiastic people who love life and charm others.',
            'description_ar': 'أشخاص عفويون ونشطون ومتحمسون يحبون الحياة ويسحرون الآخرين.',
            'strengths_en': 'Bold, original, aesthetics, showmanship, practical',
            'strengths_ar': 'جريء، أصيل، جمالي، استعراضي، عملي',
            'challenges_en': 'Sensitive, conflict-averse, easily bored, poor long-term planning, unfocused',
            'challenges_ar': 'حساس، يتجنب الصراع، يمل بسهولة، تخطيط ضعيف طويل المدى، غير مركز'
        }
    ]
    
    for pt_data in personality_types:
        existing = PersonalityType.query.filter_by(code=pt_data['code']).first()
        if not existing:
            pt = PersonalityType(**pt_data)
            db.session.add(pt)
    
    db.session.commit()
    print("✓ Initialized 16 personality types")

def initialize_career_clusters():
    """Initialize the 9 career clusters"""
    clusters = [
        {
            'name_en': 'Administration & Management',
            'name_ar': 'الإدارة والتنظيم',
            'description_en': 'Careers focused on organizing, directing, and controlling business operations and resources.',
            'description_ar': 'المهن التي تركز على تنظيم وتوجيه والتحكم في العمليات التجارية والموارد.'
        },
        {
            'name_en': 'Arts & Creative',
            'name_ar': 'الفنون والإبداع',
            'description_en': 'Careers involving creative expression, design, and artistic endeavors.',
            'description_ar': 'المهن التي تتضمن التعبير الإبداعي والتصميم والمساعي الفنية.'
        },
        {
            'name_en': 'Computer & Technology',
            'name_ar': 'الحاسوب والتكنولوجيا',
            'description_en': 'Careers in computing, software development, and information technology.',
            'description_ar': 'المهن في الحوسبة وتطوير البرمجيات وتكنولوجيا المعلومات.'
        },
        {
            'name_en': 'Education & Training',
            'name_ar': 'التعليم والتدريب',
            'description_en': 'Careers focused on teaching, training, and educational development.',
            'description_ar': 'المهن التي تركز على التدريس والتدريب والتطوير التعليمي.'
        },
        {
            'name_en': 'Engineering',
            'name_ar': 'الهندسة',
            'description_en': 'Careers in various engineering disciplines and technical problem-solving.',
            'description_ar': 'المهن في مختلف التخصصات الهندسية وحل المشاكل التقنية.'
        },
        {
            'name_en': 'Literature & Languages',
            'name_ar': 'الأدب واللغات',
            'description_en': 'Careers involving language, literature, communication, and linguistics.',
            'description_ar': 'المهن التي تتضمن اللغة والأدب والتواصل واللسانيات.'
        },
        {
            'name_en': 'Medical & Health',
            'name_ar': 'الطب والصحة',
            'description_en': 'Careers in healthcare, medicine, and health-related services.',
            'description_ar': 'المهن في الرعاية الصحية والطب والخدمات المتعلقة بالصحة.'
        },
        {
            'name_en': 'Science & Research',
            'name_ar': 'العلوم والبحث',
            'description_en': 'Careers in scientific research, analysis, and discovery.',
            'description_ar': 'المهن في البحث العلمي والتحليل والاكتشاف.'
        },
        {
            'name_en': 'Tourism & Archaeology',
            'name_ar': 'السياحة والآثار',
            'description_en': 'Careers in tourism, hospitality, archaeology, and cultural preservation.',
            'description_ar': 'المهن في السياحة والضيافة وعلم الآثار والحفاظ على الثقافة.'
        }
    ]
    
    for cluster_data in clusters:
        existing = CareerCluster.query.filter_by(name_en=cluster_data['name_en']).first()
        if not existing:
            cluster = CareerCluster(**cluster_data)
            db.session.add(cluster)
    
    db.session.commit()
    print("✓ Initialized 9 career clusters")

def initialize_pathways():
    """Initialize MOE and Mawhiba pathways"""
    pathways = [
        # MOE Pathways
        {
            'name_en': 'Sharia Track',
            'name_ar': 'المسار الشرعي',
            'source': PathwaySource.MOE,
            'description_en': 'Islamic studies and Sharia law track',
            'description_ar': 'مسار الدراسات الإسلامية والشريعة'
        },
        {
            'name_en': 'Business Administration',
            'name_ar': 'إدارة الأعمال',
            'source': PathwaySource.MOE,
            'description_en': 'Business and administrative studies track',
            'description_ar': 'مسار دراسات الأعمال والإدارة'
        },
        {
            'name_en': 'Health and Life Sciences',
            'name_ar': 'الصحة وعلوم الحياة',
            'source': PathwaySource.MOE,
            'description_en': 'Medical and biological sciences track',
            'description_ar': 'مسار العلوم الطبية والبيولوجية'
        },
        {
            'name_en': 'Science, Computers and Engineering',
            'name_ar': 'العلوم والحاسوب والهندسة',
            'source': PathwaySource.MOE,
            'description_en': 'STEM fields track',
            'description_ar': 'مسار مجالات العلوم والتكنولوجيا والهندسة والرياضيات'
        },
        {
            'name_en': 'General Path (Arts & Humanities)',
            'name_ar': 'المسار العام (الآداب والعلوم الإنسانية)',
            'source': PathwaySource.MOE,
            'description_en': 'Liberal arts and humanities track',
            'description_ar': 'مسار الآداب والعلوم الإنسانية'
        },
        # Mawhiba Pathways
        {
            'name_en': 'Medical, Biological and Chemical Sciences',
            'name_ar': 'العلوم الطبية والبيولوجية والكيميائية',
            'source': PathwaySource.MAWHIBA,
            'description_en': 'Advanced medical and life sciences for gifted students',
            'description_ar': 'العلوم الطبية وعلوم الحياة المتقدمة للطلاب الموهوبين'
        },
        {
            'name_en': 'Physics, Earth and Space Sciences',
            'name_ar': 'الفيزياء وعلوم الأرض والفضاء',
            'source': PathwaySource.MAWHIBA,
            'description_en': 'Advanced physics and space sciences for gifted students',
            'description_ar': 'الفيزياء المتقدمة وعلوم الفضاء للطلاب الموهوبين'
        },
        {
            'name_en': 'Engineering Studies',
            'name_ar': 'الدراسات الهندسية',
            'source': PathwaySource.MAWHIBA,
            'description_en': 'Advanced engineering studies for gifted students',
            'description_ar': 'الدراسات الهندسية المتقدمة للطلاب الموهوبين'
        },
        {
            'name_en': 'Computer and Applied Mathematics',
            'name_ar': 'الحاسوب والرياضيات التطبيقية',
            'source': PathwaySource.MAWHIBA,
            'description_en': 'Advanced computer science and mathematics for gifted students',
            'description_ar': 'علوم الحاسوب والرياضيات المتقدمة للطلاب الموهوبين'
        }
    ]
    
    for pathway_data in pathways:
        existing = Pathway.query.filter_by(name_en=pathway_data['name_en'], source=pathway_data['source']).first()
        if not existing:
            pathway = Pathway(**pathway_data)
            db.session.add(pathway)
    
    db.session.commit()
    print("✓ Initialized MOE and Mawhiba pathways")

def initialize_sample_questions():
    """Initialize sample assessment questions (36 questions covering all dimensions)"""
    questions = [
        # Extraversion vs Introversion (9 questions)
        {
            'order_number': 1,
            'dimension': PersonalityDimension.EI,
            'text_en': 'When facing a problem, I prefer to:',
            'text_ar': 'عند مواجهة مشكلة، أفضل أن:',
            'option_a_text_en': 'Discuss it with others to get different perspectives',
            'option_a_text_ar': 'أناقشها مع الآخرين للحصول على وجهات نظر مختلفة',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Think it through on my own first',
            'option_b_text_ar': 'أفكر فيها بمفردي أولاً'
        },
        {
            'order_number': 2,
            'dimension': PersonalityDimension.EI,
            'text_en': 'At social gatherings, I usually:',
            'text_ar': 'في التجمعات الاجتماعية، عادة ما:',
            'option_a_text_en': 'Enjoy meeting new people and engaging in conversations',
            'option_a_text_ar': 'أستمتع بلقاء أشخاص جدد والمشاركة في المحادثات',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Prefer talking to people I already know well',
            'option_b_text_ar': 'أفضل التحدث مع الأشخاص الذين أعرفهم جيداً'
        },
        {
            'order_number': 3,
            'dimension': PersonalityDimension.EI,
            'text_en': 'I tend to process my thoughts by:',
            'text_ar': 'أميل إلى معالجة أفكاري من خلال:',
            'option_a_text_en': 'Talking them out loud with others',
            'option_a_text_ar': 'التحدث عنها بصوت عالٍ مع الآخرين',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Reflecting on them quietly by myself',
            'option_b_text_ar': 'التفكير فيها بهدوء بمفردي'
        },
        
        # Sensing vs Intuition (9 questions)
        {
            'order_number': 4,
            'dimension': PersonalityDimension.SN,
            'text_en': 'When learning something new, I prefer:',
            'text_ar': 'عند تعلم شيء جديد، أفضل:',
            'option_a_text_en': 'Concrete examples and step-by-step instructions',
            'option_a_text_ar': 'أمثلة ملموسة وتعليمات خطوة بخطوة',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Abstract concepts and theoretical frameworks',
            'option_b_text_ar': 'المفاهيم المجردة والأطر النظرية'
        },
        {
            'order_number': 5,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I am more interested in:',
            'text_ar': 'أنا أكثر اهتماماً بـ:',
            'option_a_text_en': 'What is actually happening now',
            'option_a_text_ar': 'ما يحدث فعلياً الآن',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'What could be possible in the future',
            'option_b_text_ar': 'ما يمكن أن يكون ممكناً في المستقبل'
        },
        {
            'order_number': 6,
            'dimension': PersonalityDimension.SN,
            'text_en': 'When reading instructions, I:',
            'text_ar': 'عند قراءة التعليمات، أنا:',
            'option_a_text_en': 'Follow them exactly as written',
            'option_a_text_ar': 'أتبعها تماماً كما هي مكتوبة',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Use them as a general guide and adapt as needed',
            'option_b_text_ar': 'أستخدمها كدليل عام وأتكيف حسب الحاجة'
        },
        
        # Thinking vs Feeling (9 questions)
        {
            'order_number': 7,
            'dimension': PersonalityDimension.TF,
            'text_en': 'When making decisions, I prioritize:',
            'text_ar': 'عند اتخاذ القرارات، أعطي الأولوية لـ:',
            'option_a_text_en': 'Logical analysis and objective facts',
            'option_a_text_ar': 'التحليل المنطقي والحقائق الموضوعية',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Personal values and how others will be affected',
            'option_b_text_ar': 'القيم الشخصية وكيف سيتأثر الآخرون'
        },
        {
            'order_number': 8,
            'dimension': PersonalityDimension.TF,
            'text_en': 'I am more likely to:',
            'text_ar': 'من المرجح أن أكون:',
            'option_a_text_en': 'Be firm and direct in my communication',
            'option_a_text_ar': 'حازماً ومباشراً في تواصلي',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Be diplomatic and considerate of others\' feelings',
            'option_b_text_ar': 'دبلوماسياً ومراعياً لمشاعر الآخرين'
        },
        {
            'order_number': 9,
            'dimension': PersonalityDimension.TF,
            'text_en': 'When evaluating ideas, I focus more on:',
            'text_ar': 'عند تقييم الأفكار، أركز أكثر على:',
            'option_a_text_en': 'Whether they are logically sound and efficient',
            'option_a_text_ar': 'ما إذا كانت منطقية وفعالة',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Whether they align with my values and help people',
            'option_b_text_ar': 'ما إذا كانت تتماشى مع قيمي وتساعد الناس'
        },
        
        # Judging vs Perceiving (9 questions)
        {
            'order_number': 10,
            'dimension': PersonalityDimension.JP,
            'text_en': 'I prefer to:',
            'text_ar': 'أفضل أن:',
            'option_a_text_en': 'Have a clear plan and stick to it',
            'option_a_text_ar': 'أمتلك خطة واضحة وألتزم بها',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Keep my options open and adapt as I go',
            'option_b_text_ar': 'أبقي خياراتي مفتوحة وأتكيف أثناء المسير'
        },
        {
            'order_number': 11,
            'dimension': PersonalityDimension.JP,
            'text_en': 'My workspace is typically:',
            'text_ar': 'مساحة عملي عادة ما تكون:',
            'option_a_text_en': 'Organized and tidy',
            'option_a_text_ar': 'منظمة ومرتبة',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Flexible and somewhat messy',
            'option_b_text_ar': 'مرنة وفوضوية إلى حد ما'
        },
        {
            'order_number': 12,
            'dimension': PersonalityDimension.JP,
            'text_en': 'When working on projects, I:',
            'text_ar': 'عند العمل على المشاريع، أنا:',
            'option_a_text_en': 'Like to finish them well before the deadline',
            'option_a_text_ar': 'أحب إنهاءها قبل الموعد النهائي بوقت كافٍ',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Often work best under pressure near the deadline',
            'option_b_text_ar': 'غالباً ما أعمل بشكل أفضل تحت الضغط قرب الموعد النهائي'
        },
        
        # Additional questions to reach 36 total (continuing the pattern)
        # More E-I questions
        {
            'order_number': 13,
            'dimension': PersonalityDimension.EI,
            'text_en': 'After a long day, I prefer to:',
            'text_ar': 'بعد يوم طويل، أفضل أن:',
            'option_a_text_en': 'Go out and socialize with friends',
            'option_a_text_ar': 'أخرج وأتواصل اجتماعياً مع الأصدقاء',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Stay home and relax by myself',
            'option_b_text_ar': 'أبقى في المنزل وأسترخي بمفردي'
        },
        {
            'order_number': 14,
            'dimension': PersonalityDimension.EI,
            'text_en': 'In group discussions, I:',
            'text_ar': 'في المناقشات الجماعية، أنا:',
            'option_a_text_en': 'Actively participate and share my thoughts',
            'option_a_text_ar': 'أشارك بنشاط وأشارك أفكاري',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Listen carefully and contribute when asked',
            'option_b_text_ar': 'أستمع بعناية وأساهم عندما يُطلب مني'
        },
        {
            'order_number': 15,
            'dimension': PersonalityDimension.EI,
            'text_en': 'I get energized by:',
            'text_ar': 'أحصل على الطاقة من:',
            'option_a_text_en': 'Being around other people',
            'option_a_text_ar': 'التواجد حول الآخرين',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Having quiet time alone',
            'option_b_text_ar': 'قضاء وقت هادئ بمفردي'
        },
        
        # More S-N questions
        {
            'order_number': 16,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I trust:',
            'text_ar': 'أثق في:',
            'option_a_text_en': 'My experience and proven methods',
            'option_a_text_ar': 'خبرتي والطرق المجربة',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'My intuition and new possibilities',
            'option_b_text_ar': 'حدسي والإمكانيات الجديدة'
        },
        {
            'order_number': 17,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I prefer to focus on:',
            'text_ar': 'أفضل التركيز على:',
            'option_a_text_en': 'Details and specifics',
            'option_a_text_ar': 'التفاصيل والخصوصيات',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'The big picture and overall patterns',
            'option_b_text_ar': 'الصورة الكبيرة والأنماط العامة'
        },
        {
            'order_number': 18,
            'dimension': PersonalityDimension.SN,
            'text_en': 'When solving problems, I:',
            'text_ar': 'عند حل المشاكل، أنا:',
            'option_a_text_en': 'Use tried and tested approaches',
            'option_a_text_ar': 'أستخدم الطرق المجربة والمختبرة',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Look for innovative and creative solutions',
            'option_b_text_ar': 'أبحث عن حلول مبتكرة وإبداعية'
        },
        
        # More T-F questions
        {
            'order_number': 19,
            'dimension': PersonalityDimension.TF,
            'text_en': 'I am more motivated by:',
            'text_ar': 'أنا أكثر تحفيزاً بـ:',
            'option_a_text_en': 'Achievement and competence',
            'option_a_text_ar': 'الإنجاز والكفاءة',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Harmony and helping others',
            'option_b_text_ar': 'الانسجام ومساعدة الآخرين'
        },
        {
            'order_number': 20,
            'dimension': PersonalityDimension.TF,
            'text_en': 'When giving feedback, I:',
            'text_ar': 'عند تقديم التغذية الراجعة، أنا:',
            'option_a_text_en': 'Focus on what needs to be improved',
            'option_a_text_ar': 'أركز على ما يحتاج إلى تحسين',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Consider how the person might feel',
            'option_b_text_ar': 'أراعي كيف قد يشعر الشخص'
        },
        {
            'order_number': 21,
            'dimension': PersonalityDimension.TF,
            'text_en': 'I value:',
            'text_ar': 'أقدر:',
            'option_a_text_en': 'Fairness and justice',
            'option_a_text_ar': 'العدالة والإنصاف',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Compassion and understanding',
            'option_b_text_ar': 'الرحمة والتفهم'
        },
        
        # More J-P questions
        {
            'order_number': 22,
            'dimension': PersonalityDimension.JP,
            'text_en': 'I prefer to:',
            'text_ar': 'أفضل أن:',
            'option_a_text_en': 'Make decisions quickly and move forward',
            'option_a_text_ar': 'أتخذ القرارات بسرعة وأمضي قدماً',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Keep gathering information before deciding',
            'option_b_text_ar': 'أستمر في جمع المعلومات قبل اتخاذ القرار'
        },
        {
            'order_number': 23,
            'dimension': PersonalityDimension.JP,
            'text_en': 'My approach to deadlines is:',
            'text_ar': 'نهجي مع المواعيد النهائية هو:',
            'option_a_text_en': 'Plan ahead and finish early',
            'option_a_text_ar': 'التخطيط مسبقاً والانتهاء مبكراً',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Work steadily and finish just in time',
            'option_b_text_ar': 'العمل بثبات والانتهاء في الوقت المناسب'
        },
        {
            'order_number': 24,
            'dimension': PersonalityDimension.JP,
            'text_en': 'I feel more comfortable when:',
            'text_ar': 'أشعر بالراحة أكثر عندما:',
            'option_a_text_en': 'Things are settled and decided',
            'option_a_text_ar': 'تكون الأمور مستقرة ومحسومة',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Options remain open and flexible',
            'option_b_text_ar': 'تبقى الخيارات مفتوحة ومرنة'
        },
        
        # Final set of questions to complete 36
        {
            'order_number': 25,
            'dimension': PersonalityDimension.EI,
            'text_en': 'When learning in a group, I:',
            'text_ar': 'عند التعلم في مجموعة، أنا:',
            'option_a_text_en': 'Enjoy discussing ideas with others',
            'option_a_text_ar': 'أستمتع بمناقشة الأفكار مع الآخرين',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Prefer to work through ideas independently first',
            'option_b_text_ar': 'أفضل العمل على الأفكار بشكل مستقل أولاً'
        },
        {
            'order_number': 26,
            'dimension': PersonalityDimension.EI,
            'text_en': 'I tend to:',
            'text_ar': 'أميل إلى:',
            'option_a_text_en': 'Think out loud',
            'option_a_text_ar': 'التفكير بصوت عالٍ',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Think before speaking',
            'option_b_text_ar': 'التفكير قبل التحدث'
        },
        {
            'order_number': 27,
            'dimension': PersonalityDimension.EI,
            'text_en': 'I am energized by:',
            'text_ar': 'أحصل على الطاقة من:',
            'option_a_text_en': 'Variety and action',
            'option_a_text_ar': 'التنوع والعمل',
            'option_a_maps_to_first': True,  # E
            'option_b_text_en': 'Quiet and reflection',
            'option_b_text_ar': 'الهدوء والتأمل'
        },
        
        {
            'order_number': 28,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I am more interested in:',
            'text_ar': 'أنا أكثر اهتماماً بـ:',
            'option_a_text_en': 'Facts and reality',
            'option_a_text_ar': 'الحقائق والواقع',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Ideas and possibilities',
            'option_b_text_ar': 'الأفكار والإمكانيات'
        },
        {
            'order_number': 29,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I prefer to work with:',
            'text_ar': 'أفضل العمل مع:',
            'option_a_text_en': 'Concrete information',
            'option_a_text_ar': 'المعلومات الملموسة',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Abstract concepts',
            'option_b_text_ar': 'المفاهيم المجردة'
        },
        {
            'order_number': 30,
            'dimension': PersonalityDimension.SN,
            'text_en': 'I am drawn to:',
            'text_ar': 'أنجذب إلى:',
            'option_a_text_en': 'Practical applications',
            'option_a_text_ar': 'التطبيقات العملية',
            'option_a_maps_to_first': True,  # S
            'option_b_text_en': 'Theoretical frameworks',
            'option_b_text_ar': 'الأطر النظرية'
        },
        
        {
            'order_number': 31,
            'dimension': PersonalityDimension.TF,
            'text_en': 'When making decisions, I consider:',
            'text_ar': 'عند اتخاذ القرارات، أراعي:',
            'option_a_text_en': 'Logical consequences',
            'option_a_text_ar': 'العواقب المنطقية',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Impact on people',
            'option_b_text_ar': 'التأثير على الناس'
        },
        {
            'order_number': 32,
            'dimension': PersonalityDimension.TF,
            'text_en': 'I am more convinced by:',
            'text_ar': 'أنا أكثر اقتناعاً بـ:',
            'option_a_text_en': 'Logical arguments',
            'option_a_text_ar': 'الحجج المنطقية',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Emotional appeals',
            'option_b_text_ar': 'النداءات العاطفية'
        },
        {
            'order_number': 33,
            'dimension': PersonalityDimension.TF,
            'text_en': 'I prefer to be:',
            'text_ar': 'أفضل أن أكون:',
            'option_a_text_en': 'Objective and impartial',
            'option_a_text_ar': 'موضوعياً ومحايداً',
            'option_a_maps_to_first': True,  # T
            'option_b_text_en': 'Personal and caring',
            'option_b_text_ar': 'شخصياً ومهتماً'
        },
        
        {
            'order_number': 34,
            'dimension': PersonalityDimension.JP,
            'text_en': 'I like to:',
            'text_ar': 'أحب أن:',
            'option_a_text_en': 'Have things decided',
            'option_a_text_ar': 'تكون الأمور محسومة',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Keep options open',
            'option_b_text_ar': 'أبقي الخيارات مفتوحة'
        },
        {
            'order_number': 35,
            'dimension': PersonalityDimension.JP,
            'text_en': 'I work better with:',
            'text_ar': 'أعمل بشكل أفضل مع:',
            'option_a_text_en': 'Clear structure and deadlines',
            'option_a_text_ar': 'هيكل واضح ومواعيد نهائية',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Flexibility and spontaneity',
            'option_b_text_ar': 'المرونة والعفوية'
        },
        {
            'order_number': 36,
            'dimension': PersonalityDimension.JP,
            'text_en': 'My lifestyle is more:',
            'text_ar': 'أسلوب حياتي أكثر:',
            'option_a_text_en': 'Structured and planned',
            'option_a_text_ar': 'منظماً ومخططاً',
            'option_a_maps_to_first': True,  # J
            'option_b_text_en': 'Flexible and adaptable',
            'option_b_text_ar': 'مرناً وقابلاً للتكيف'
        }
    ]
    
    for question_data in questions:
        existing = Question.query.filter_by(order_number=question_data['order_number']).first()
        if not existing:
            question = Question(**question_data)
            db.session.add(question)
    
    db.session.commit()
    print("✓ Initialized 36 assessment questions")

def initialize_system_configurations():
    """Initialize default system configurations"""
    configs = [
        {
            'key': 'default_language',
            'value': 'en',
            'description': 'Default language for the system (en/ar)',
            'deployment_mode': None
        },
        {
            'key': 'max_concurrent_sessions',
            'value': '500000',
            'description': 'Maximum number of concurrent assessment sessions',
            'deployment_mode': None
        },
        {
            'key': 'session_timeout_minutes',
            'value': '60',
            'description': 'Session timeout in minutes',
            'deployment_mode': None
        },
        {
            'key': 'report_logo_url_standard',
            'value': '/static/images/masark_logo.png',
            'description': 'Logo URL for standard deployment mode',
            'deployment_mode': DeploymentMode.STANDARD
        },
        {
            'key': 'report_logo_url_mawhiba',
            'value': '/static/images/mawhiba_logo.png',
            'description': 'Logo URL for Mawhiba deployment mode',
            'deployment_mode': DeploymentMode.MAWHIBA
        },
        {
            'key': 'report_primary_color_standard',
            'value': '#2563eb',
            'description': 'Primary color for standard deployment reports',
            'deployment_mode': DeploymentMode.STANDARD
        },
        {
            'key': 'report_primary_color_mawhiba',
            'value': '#059669',
            'description': 'Primary color for Mawhiba deployment reports',
            'deployment_mode': DeploymentMode.MAWHIBA
        },
        {
            'key': 'top_careers_to_show',
            'value': '10',
            'description': 'Number of top career matches to show in reports',
            'deployment_mode': None
        }
    ]
    
    for config_data in configs:
        existing = SystemConfiguration.query.filter_by(key=config_data['key']).first()
        if not existing:
            config = SystemConfiguration(**config_data)
            db.session.add(config)
    
    db.session.commit()
    print("✓ Initialized system configurations")

def create_default_admin():
    """Create default admin user"""
    existing_admin = AdminUser.query.filter_by(username='admin').first()
    if not existing_admin:
        admin = AdminUser(
            username='admin',
            email='admin@masark.com',
            password_hash=generate_password_hash('admin123'),  # Default password - should be changed
            first_name='System',
            last_name='Administrator',
            is_super_admin=True,
            role='super_admin'
        )
        db.session.add(admin)
        db.session.commit()
        print("✓ Created default admin user (username: admin, password: admin123)")
    else:
        print("✓ Default admin user already exists")

def initialize_database():
    """Main function to initialize all database data"""
    print("Initializing Masark database with seed data...")
    
    try:
        initialize_personality_types()
        initialize_career_clusters()
        initialize_pathways()
        initialize_sample_questions()
        initialize_system_configurations()
        create_default_admin()
        
        print("\n✅ Database initialization completed successfully!")
        print("📊 Summary:")
        print(f"   - 16 personality types")
        print(f"   - 9 career clusters")
        print(f"   - 9 pathways (5 MOE + 4 Mawhiba)")
        print(f"   - 36 assessment questions")
        print(f"   - System configurations")
        print(f"   - Default admin user")
        print("\n🔐 Default admin credentials:")
        print("   Username: admin")
        print("   Password: admin123")
        print("   ⚠️  Please change the default password after first login!")
        
    except Exception as e:
        print(f"❌ Error during database initialization: {str(e)}")
        db.session.rollback()
        raise

if __name__ == '__main__':
    # This script should be run within the Flask application context
    print("This script should be imported and run within the Flask app context")



def main():
    """Main function to run database initialization with Flask app context"""
    app = Flask(__name__)
    app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///masark.db'
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
    
    db.init_app(app)
    
    with app.app_context():
        print("🚀 Starting database initialization...")
        
        # Create all tables
        db.create_all()
        print("✅ Database tables created")
        
        # Initialize data
        initialize_personality_types()
        initialize_career_clusters()
        initialize_pathways()
        initialize_sample_questions()
        initialize_system_configurations()
        
        print("🎉 Database initialization completed successfully!")

if __name__ == "__main__":
    main()

