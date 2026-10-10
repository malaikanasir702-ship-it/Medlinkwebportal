-- ============================================================
-- MedLink — 500 Medicines SQL Seed
-- Safe to run multiple times (INSERT ... ON CONFLICT DO NOTHING)
-- Table: "Medicines"  |  PostgreSQL / Supabase
-- ============================================================

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
-- ═══ FEVER (20) ═══
('Panadol 500mg Tablets','GSK','Fever','Fast relief of fever, headache and body aches.',65,1200,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Calpol 250mg Suspension 60ml','GSK','Fever','Paediatric paracetamol suspension for fever and mild pain.',145,400,'2028-04-01',false,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Disprin 300mg Soluble Tablets','Reckitt','Fever','Aspirin soluble tablets for rapid fever and pain relief.',50,800,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Paracetamol Extra 500mg/65mg','Getz Pharma','Fever','Extra strength paracetamol with caffeine for fever and migraine.',90,650,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Panadol CF Day/Night Tablets','GSK','Fever','Day and night formula for cold flu fever and congestion relief.',120,550,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Febrilix 120mg/5ml Syrup 90ml','Sami','Fever','Ibuprofen suspension for paediatric fever management.',110,350,'2028-04-01',false,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Tylenol 650mg Extended Release','J&J','Fever','Extended release paracetamol providing 8 hour fever control.',180,300,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Meftal-P Drops 15ml','Blue Cross','Fever','Mefenamic acid drops for infants and toddlers with fever.',95,280,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Ibuprofen 200mg Tablets','Getz Pharma','Fever','OTC ibuprofen for mild to moderate fever and pain.',70,900,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Panadol Extend 665mg','GSK','Fever','Prolonged release formula for sustained fever control.',160,420,'2028-04-01',false,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Brufen Syrup 100mg/5ml','Abbott','Fever','Ibuprofen syrup for children with fever above 38C.',165,310,'2028-04-01',false,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Aspirin 75mg Dispersible','Bayer','Fever','Low-dose aspirin dispersible tablets for fever and pain.',45,700,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Nurofen 400mg Tablets','Reckitt','Fever','Ibuprofen for effective fever headache and toothache relief.',130,500,'2028-04-01',false,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Panadol Baby 80mg Suppositories','GSK','Fever','Rectal paracetamol suppositories for infants unable to swallow.',200,180,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Naproxen 250mg Tablets','Sanofi','Fever','NSAID for fever pain and inflammatory conditions.',150,400,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Combiflam Ibuprofen+Paracetamol','Sanofi','Fever','Dual-action fever and pain tablet combination.',95,600,'2028-04-01',false,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Panadol Fizzy 500mg Effervescent','GSK','Fever','Effervescent paracetamol for fast fever dissolution.',140,350,'2028-04-01',false,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Metacin 500mg Tablets','Smith Kline','Fever','Paracetamol tablets for effective fever and pain management.',60,750,'2028-04-01',false,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Ibugesic Plus Syrup 100ml','Cipla','Fever','Ibuprofen and paracetamol combination syrup for children.',175,290,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Coldrex HotRem Sachets','GSK','Fever','Hot lemon drink for relief of fever aches and cold symptoms.',85,420,'2028-04-01',false,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
-- ═══ PAIN (25) ═══
('Brufen 400mg Ibuprofen','Abbott','Pain','Anti-inflammatory for muscular dental and arthritic pain.',120,950,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Ponstan Forte 500mg','Pfizer','Pain','Mefenamic acid for period cramps and post-surgical pain.',180,600,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Nuberol Forte','Searle','Pain','Paracetamol plus orphenadrine muscle relaxant for back pain.',220,750,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Voltral Emulgel 1% 50g','Novartis','Pain','Topical diclofenac gel for localised joint inflammation.',310,350,'2028-04-01',false,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Celebrex 200mg Celecoxib','Pfizer','Pain','COX-2 inhibitor for osteoarthritis and rheumatoid arthritis pain.',980,250,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Naproxen Sodium 550mg','Roche','Pain','Long-acting NSAID for musculoskeletal and dental pain.',210,400,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Ketorol 10mg Ketorolac','Sun Pharma','Pain','Short-term moderate to severe post-operative pain relief.',150,500,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Tramol 50mg Tramadol','Sami','Pain','Opioid analgesic for moderate to severe acute pain.',280,300,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Feldene 20mg Piroxicam','Pfizer','Pain','Long-acting NSAID for arthritis and musculoskeletal pain.',190,350,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Dicloran 50mg Tablets','Getz Pharma','Pain','Diclofenac sodium for pain swelling and inflammation.',110,600,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Voltaren 75mg Injection','Novartis','Pain','Diclofenac sodium injection for acute severe pain.',380,200,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Lyrica 75mg Pregabalin','Pfizer','Pain','Neuropathic pain management and fibromyalgia treatment.',1850,180,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Neurontin 300mg Gabapentin','Pfizer','Pain','Anticonvulsant for neuropathic pain and nerve damage.',1200,220,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Muscle Ease Gel 100g','Reckitt','Pain','Cooling menthol gel for muscle aches sprains and strains.',350,280,'2028-04-01',false,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Iodex Ultra Gel 30g','GSK','Pain','Topical pain relief gel with methyl salicylate.',220,320,'2028-04-01',false,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Skelan 8mg Eperisone','Searle','Pain','Muscle relaxant for spasticity and lower back pain.',480,200,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Brufen Retard 800mg','Abbott','Pain','Slow-release ibuprofen for chronic pain management.',260,300,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Codeine Phosphate 30mg','GSK','Pain','Opioid analgesic for moderate pain combined with paracetamol.',320,180,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Flurbiprofen 50mg Tabs','Knoll','Pain','NSAID for dental postoperative and menstrual pain.',140,400,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Indocin 25mg Indomethacin','MSD','Pain','Potent NSAID for gout attacks and ankylosing spondylitis.',160,300,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Mobic 7.5mg Meloxicam','Boehringer','Pain','Selective COX-2 NSAID for osteoarthritis with less GI side effects.',480,250,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Ultracet Tramadol+Paracetamol','J&J','Pain','Combined opioid and non-opioid tablet for moderate severe pain.',420,280,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Carisoprodol 350mg','Sun Pharma','Pain','Skeletal muscle relaxant for acute musculoskeletal pain.',560,180,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Piroxicam Gel 0.5% 60g','Getz Pharma','Pain','Topical piroxicam gel for joint and soft tissue pain.',175,280,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
-- ═══ ANTIBIOTICS (30) ═══
('Augmentin 625mg','GSK','Antibiotics','Amoxicillin/Clavulanic acid for respiratory and ENT infections.',560,700,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Ciproxin 500mg Ciprofloxacin','Bayer','Antibiotics','Fluoroquinolone for UTI GI and skin bacterial infections.',480,400,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Azomax 500mg Azithromycin','Getz Pharma','Antibiotics','Macrolide for chest throat and sinus infections.',390,650,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Amoxil 500mg Amoxicillin','GSK','Antibiotics','Broad-spectrum penicillin for common bacterial infections.',180,900,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Flagyl 400mg Metronidazole','Sanofi','Antibiotics','Antiprotozoal and antibacterial for GI and anaerobic infections.',95,1100,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Doxycycline 100mg Capsules','Pfizer','Antibiotics','Tetracycline antibiotic for chest tick-borne and acne infections.',220,500,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Ceftriaxone 1g Injection','Roche','Antibiotics','3rd generation cephalosporin injection for severe infections.',850,300,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Cephalexin 500mg Capsules','Sami','Antibiotics','First-gen cephalosporin for skin UTI and respiratory infections.',280,420,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Clindamycin 300mg Capsules','Pfizer','Antibiotics','Lincosamide antibiotic for dental bone and soft tissue infections.',480,280,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Erythromycin 500mg Tablets','Abbott','Antibiotics','Macrolide antibiotic alternative for penicillin-allergic patients.',160,600,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Nitrofurantoin 100mg Capsules','MSD','Antibiotics','Urinary antiseptic for uncomplicated lower UTI treatment.',380,350,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Trimethoprim 200mg Tablets','Sami','Antibiotics','Antifolate antibiotic for UTI and respiratory tract infections.',150,500,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Roxithromycin 150mg Tablets','Sanofi','Antibiotics','Macrolide for respiratory and soft tissue bacterial infections.',340,380,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Cefuroxime 500mg Tablets','GSK','Antibiotics','2nd gen cephalosporin for sinusitis bronchitis and skin infections.',620,300,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Levofloxacin 500mg Tablets','J&J','Antibiotics','Fluoroquinolone for CAP UTI and complicated skin infections.',580,350,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Moxifloxacin 400mg Tablets','Bayer','Antibiotics','4th gen fluoroquinolone for community-acquired pneumonia.',780,220,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Cefixime 400mg Capsules','Sami','Antibiotics','3rd gen oral cephalosporin for ENT and urinary infections.',420,320,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Ampicillin 500mg Capsules','GSK','Antibiotics','Broad-spectrum penicillin for respiratory and GI infections.',130,500,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Rifampicin 600mg Capsules','Sanofi','Antibiotics','Rifamycin antibiotic for tuberculosis and leprosy treatment.',480,280,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Chloramphenicol Eye Drops 5ml','Martindale','Antibiotics','Broad-spectrum antibiotic eye drops for bacterial conjunctivitis.',180,400,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Fusidic Acid Cream 2% 15g','Leo Pharma','Antibiotics','Topical antibiotic for skin infections and infected eczema.',320,300,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Neomycin Polymyxin Drops','Alcon','Antibiotics','Combination antibiotic drops for external ear and eye infections.',260,280,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Fosfomycin 3g Granules','Zambon','Antibiotics','Single-dose antibiotic sachet for uncomplicated cystitis.',1200,200,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Amikacin 500mg Injection','Bristol','Antibiotics','Aminoglycoside injection for gram-negative septicaemia.',580,180,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Gentamicin 80mg Injection','Schering','Antibiotics','Aminoglycoside for serious gram-negative bacterial infections.',220,250,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Vancomycin 500mg Injection','Eli Lilly','Antibiotics','Glycopeptide for MRSA and resistant gram-positive infections.',3500,80,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
-- ═══ CARDIAC (25) ═══
('Cardinit 2.6mg CR Tablets','Searle','Cardiac','Glyceryl trinitrate for angina pectoris prevention.',280,400,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Lowplat 75mg Clopidogrel','Getz Pharma','Cardiac','Antiplatelet agent preventing thrombotic cardiovascular events.',340,600,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Lopresor 50mg Metoprolol','Novartis','Cardiac','Beta-blocker for hypertension angina and arrhythmia.',210,350,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Concor 5mg Bisoprolol','Merck','Cardiac','Cardioselective beta-blocker for heart failure and hypertension.',480,550,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Zocor 20mg Simvastatin','MSD','Cardiac','Statin for reducing LDL cholesterol and cardiovascular risk.',620,400,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Lipitor 20mg Atorvastatin','Pfizer','Cardiac','High-potency statin for hypercholesterolaemia and CVD prevention.',850,500,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Crestor 10mg Rosuvastatin','AstraZeneca','Cardiac','Statin for mixed dyslipidaemia and cardiovascular risk reduction.',980,350,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Norvasc 5mg Amlodipine','Pfizer','Cardiac','Calcium channel blocker for hypertension and stable angina.',360,600,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Coversyl 5mg Perindopril','Servier','Cardiac','ACE inhibitor for hypertension and heart failure management.',520,450,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Diovan 80mg Valsartan','Novartis','Cardiac','ARB for hypertension and post-myocardial infarction management.',720,350,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Lasix 40mg Furosemide','Sanofi','Cardiac','Loop diuretic for oedema associated with heart failure.',85,700,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Aldactone 25mg Spironolactone','Pfizer','Cardiac','Potassium-sparing diuretic for heart failure and hypertension.',180,400,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Digoxin 0.25mg Tablets','GSK','Cardiac','Cardiac glycoside for heart failure and atrial fibrillation.',120,350,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Amiodarone 200mg Tablets','Sanofi','Cardiac','Class III antiarrhythmic for life-threatening ventricular arrhythmias.',680,200,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Warfarin 5mg Tablets','Bristol','Cardiac','Anticoagulant for DVT PE and atrial fibrillation.',150,300,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Tenormin 50mg Atenolol','AstraZeneca','Cardiac','Beta-blocker for hypertension angina and myocardial infarction.',160,550,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Ramipril 5mg Capsules','Sanofi','Cardiac','ACE inhibitor for hypertension heart failure and post-MI protection.',380,400,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Minipress 1mg Prazosin','Pfizer','Cardiac','Alpha-blocker for hypertension and benign prostatic hyperplasia.',195,300,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Isoptin 80mg Verapamil','Abbott','Cardiac','Calcium channel blocker for hypertension and supraventricular arrhythmias.',280,300,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Trental 400mg Pentoxifylline','Sanofi','Cardiac','Improves microcirculation in peripheral vascular disease.',420,250,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Nitrocontin 2.6mg CR','AstraZeneca','Cardiac','Long-acting isosorbide mononitrate for chronic angina prevention.',240,350,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Eplerenone 25mg Tablets','Pfizer','Cardiac','Selective aldosterone blocker for heart failure post-MI.',1850,150,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Hydralazine 25mg Tablets','Shire','Cardiac','Vasodilator for hypertensive emergencies and heart failure.',220,250,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Pradaxa 150mg Dabigatran','Boehringer','Cardiac','NOAC for stroke prevention in non-valvular atrial fibrillation.',3600,120,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Xarelto 20mg Rivaroxaban','Bayer','Cardiac','Factor Xa inhibitor for DVT treatment and AF stroke prevention.',4200,100,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
-- ═══ DIABETES (25) ═══
('Glucophage 500mg Metformin','Merck','Diabetes','First-line biguanide for type 2 diabetes glycaemic control.',160,900,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Getryl 2mg Glimepiride','Getz Pharma','Diabetes','Sulphonylurea stimulating pancreatic insulin secretion.',290,600,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Januvia 100mg Sitagliptin','MSD','Diabetes','DPP-4 inhibitor regulating post-meal blood glucose.',2400,250,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Jardiance 10mg Empagliflozin','Boehringer','Diabetes','SGLT-2 inhibitor reducing glucose and cardiovascular risk.',5200,150,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Forxiga 10mg Dapagliflozin','AstraZeneca','Diabetes','SGLT-2 inhibitor for type 2 diabetes and heart failure.',4800,180,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Lantus 100IU/ml Insulin Glargine','Sanofi','Diabetes','Long-acting basal insulin for type 1 and 2 diabetes.',2800,200,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Novorapid 100IU/ml FlexPen','Novo Nordisk','Diabetes','Rapid-acting insulin analogue for mealtime glucose control.',2400,220,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Humulin 30/70 100IU/ml','Eli Lilly','Diabetes','Biphasic human insulin for type 1 and 2 diabetes.',1800,350,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Glucobay 50mg Acarbose','Bayer','Diabetes','Alpha-glucosidase inhibitor slowing post-meal glucose rise.',380,300,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Gliclazide MR 60mg Tablets','Servier','Diabetes','Modified-release sulphonylurea with lower hypoglycaemia risk.',340,400,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Tradjenta 5mg Linagliptin','Boehringer','Diabetes','DPP-4 inhibitor safe for patients with renal impairment.',2800,180,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Glucovance Metformin+Glibenclamide','Merck','Diabetes','Combination tablet for type 2 diabetes dual-action control.',320,400,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Galvus 50mg Vildagliptin','Novartis','Diabetes','DPP-4 inhibitor for blood sugar management in type 2 diabetes.',1800,200,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Pioglitazone 30mg Tablets','Takeda','Diabetes','Thiazolidinedione improving insulin sensitivity in type 2 diabetes.',520,250,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Repaglinide 1mg Tablets','Novo Nordisk','Diabetes','Meglitinide insulin secretagogue for postprandial glucose control.',680,200,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Amaryl 1mg Glimepiride','Sanofi','Diabetes','Once-daily sulphonylurea for type 2 diabetes.',260,500,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Gluformin 1000mg SR Tablets','Sami','Diabetes','Sustained-release metformin reducing GI side effects.',280,450,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Glucoject Lancets 30G Box 100','AgaMatrix','Diabetes','Ultra-fine lancets for comfortable blood glucose monitoring.',480,600,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Byetta 5mcg Injection Exenatide','AstraZeneca','Diabetes','Twice-daily GLP-1 receptor agonist for type 2 diabetes.',9800,80,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Ozempic 0.5mg Injection','Novo Nordisk','Diabetes','Weekly GLP-1 agonist for type 2 diabetes and weight loss.',18000,60,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
-- ═══ GASTRIC (25) ═══
('Risek 40mg Omeprazole','Getz Pharma','Gastric','Proton pump inhibitor for GERD acidity and peptic ulcers.',420,800,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Gaviscon Double Action Syrup 150ml','Reckitt','Gastric','Antacid forming protective raft against acid reflux.',290,600,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Nexum 40mg Esomeprazole','Getz Pharma','Gastric','Advanced acid suppression for reflux oesophagitis.',510,500,'2028-04-01',false,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Motilium 10mg Domperidone','J&J','Gastric','Prokinetic for nausea vomiting and gastroparesis.',180,700,'2028-04-01',false,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Zantac 150mg Ranitidine','GSK','Gastric','H2-blocker for heartburn GERD and gastric ulcer healing.',140,600,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Lopermide 2mg Loperamide','J&J','Gastric','Antidiarrhoeal reducing intestinal motility in acute diarrhoea.',80,800,'2028-04-01',false,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Buscopan 10mg Hyoscine','Boehringer','Gastric','Antispasmodic for abdominal cramps IBS and renal colic.',220,500,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Duphalac Lactulose Syrup 200ml','Abbott','Gastric','Osmotic laxative for chronic constipation.',380,400,'2028-04-01',false,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Colpermin Peppermint Oil Caps','Tillotts','Gastric','Enteric-coated peppermint oil for IBS-related pain and bloating.',520,250,'2028-04-01',false,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Normix 200mg Rifaximin','Alfa Wassermann','Gastric','Non-systemic antibiotic for travellers diarrhoea and IBS-D.',1800,180,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Mylanta Antacid Liquid 200ml','J&J','Gastric','Aluminium hydroxide and magnesium antacid for fast heartburn relief.',220,500,'2028-04-01',false,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Eno Fruit Salt Sachet','GSK','Gastric','Effervescent antacid granules for rapid indigestion relief.',35,1500,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Panto 40mg Pantoprazole','Sun Pharma','Gastric','PPI for GERD and Zollinger-Ellison syndrome management.',380,600,'2028-04-01',false,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Lansoprazole 30mg Capsules','Takeda','Gastric','PPI for peptic ulcer disease and NSAID-induced gastritis.',450,400,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Rabeprazole 20mg Tablets','Eisai','Gastric','PPI with rapid onset for GERD and H pylori eradication.',490,380,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Sucralfate 1g Suspension 200ml','Pfizer','Gastric','Cytoprotective agent forming mucosal barrier over ulcer site.',320,350,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Reglan 10mg Metoclopramide','Sanofi','Gastric','Prokinetic and antiemetic for gastroparesis and nausea.',95,600,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Dulcolax 5mg Bisacodyl','Boehringer','Gastric','Stimulant laxative for constipation and bowel preparation.',180,500,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Heliox Sachet Triple Therapy','Getz Pharma','Gastric','Helicobacter pylori eradication combination therapy.',1200,280,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Normacol Plus Granules 500g','Norgine','Gastric','Stimulant bulk-forming laxative for chronic constipation.',680,200,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Imodium Plus Comfort 2mg','J&J','Gastric','Loperamide with simethicone for diarrhoea and bloating.',280,350,'2028-04-01',false,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Smecta 3g Sachets','Ipsen','Gastric','Diosmectite for acute diarrhoea and irritable bowel syndrome.',120,400,'2028-04-01',false,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Librax Chlordiazepoxide+Clidinium','Roche','Gastric','Combined anxiolytic-antispasmodic for IBS and functional GI disorders.',280,350,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Misoprostol 200mcg Tablets','Pfizer','Gastric','Prostaglandin analogue for NSAID-induced peptic ulcer prevention.',420,200,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
-- ═══ RESPIRATORY (25) ═══
('Ventolin Inhaler 100mcg','GSK','Respiratory','Fast-acting salbutamol bronchodilator for asthma relief.',350,400,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Pulmonol Cough Syrup 120ml','Ccl Pharma','Respiratory','Expectorant syrup for chest congestion and persistent cough.',180,600,'2028-04-01',false,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Montika 10mg Montelukast','Sami','Respiratory','Leukotriene antagonist for asthma and allergic rhinitis.',420,550,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Seretide 250/25 Evohaler','GSK','Respiratory','Fluticasone/salmeterol combination inhaler for persistent asthma.',2800,180,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Pulmicort 200mcg Turbuhaler','AstraZeneca','Respiratory','Budesonide dry-powder inhaler for asthma prevention.',1800,200,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Spiriva Handihaler 18mcg','Boehringer','Respiratory','Tiotropium once-daily LAMA inhaler for COPD maintenance.',3200,150,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Atrovent 20mcg Inhaler','Boehringer','Respiratory','Ipratropium bromide for COPD and acute bronchospasm.',1400,220,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Singulair 5mg Chewable Tabs','MSD','Respiratory','Paediatric leukotriene receptor antagonist for asthma.',1950,280,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Beclate 250mcg Inhaler','Cipla','Respiratory','Beclomethasone inhaler for daily asthma and rhinitis control.',950,300,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Mucinex 600mg ER Guaifenesin','Reckitt','Respiratory','Extended-release expectorant thinning and loosening mucus.',420,350,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Rhinocort Aqua 64mcg Spray','AstraZeneca','Respiratory','Budesonide nasal spray for allergic and non-allergic rhinitis.',680,280,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Bromhexine 8mg Tablets','Boehringer','Respiratory','Mucolytic reducing sputum viscosity in productive cough.',95,600,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Ambroxol 30mg Tablets','Sanofi','Respiratory','Mucolytic and secretolytic agent for COPD and bronchitis.',110,700,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Dextromethorphan 15mg Syrup 120ml','Reckitt','Respiratory','Non-narcotic antitussive for dry irritating cough.',140,500,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Prednisolone 5mg Tablets','Sanofi','Respiratory','Corticosteroid for asthma exacerbations and severe allergic reactions.',75,800,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Theophylline SR 300mg Tablets','Sanofi','Respiratory','Methylxanthine bronchodilator for COPD and refractory asthma.',180,350,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('N-Acetylcysteine 600mg Effervescent','Zambon','Respiratory','Mucolytic and antioxidant for chronic bronchitis and COPD.',380,350,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Formoterol 12mcg Turbuhaler','AstraZeneca','Respiratory','Long-acting beta-2 agonist for COPD and asthma maintenance.',1600,200,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Carbocisteine 375mg Capsules','Sanofi','Respiratory','Mucolytic reducing mucus viscosity in COPD and bronchiectasis.',260,400,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Ipratropium 500mcg Nebules','Boehringer','Respiratory','Nebuliser solution for acute COPD and asthma exacerbations.',350,280,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Codeine Linctus 200ml','GSK','Respiratory','Opioid antitussive syrup for severe unproductive cough.',280,250,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Fluticasone 500mcg/Salmeterol 50mcg','GSK','Respiratory','High-strength combination inhaler for severe persistent asthma.',3600,120,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Zafirlukast 20mg Tablets','AstraZeneca','Respiratory','Leukotriene antagonist for mild to moderate asthma.',680,200,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Flixonase 50mcg Nasal Spray','GSK','Respiratory','Fluticasone propionate for persistent allergic rhinitis.',780,240,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Nasacort Aqua 55mcg Spray','Sanofi','Respiratory','Triamcinolone nasal spray for seasonal allergic rhinitis.',720,260,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
-- ═══ SKIN CARE (20) ═══
('Betnovate-N Cream 20g','GSK','Skin Care','Betamethasone plus neomycin for eczema and infected dermatitis.',140,500,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Hydrozole Cream 20g','Stiefel','Skin Care','Hydrocortisone plus clotrimazole for inflamed fungal infections.',220,350,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Sunblock SPF 60 Gel 50ml','Dermacos','Skin Care','Broad-spectrum UVA/UVB sunscreen for daily photoprotection.',850,200,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Clindac-A Gel 20g Clindamycin','Galderma','Skin Care','Topical antibiotic gel for inflammatory and comedonal acne.',380,300,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Tretinoin 0.025% Cream 20g','J&J','Skin Care','Retinoid for acne vulgaris fine lines and photo-damaged skin.',420,250,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Nizoral 2% Shampoo 100ml','J&J','Skin Care','Ketoconazole shampoo for dandruff and seborrhoeic dermatitis.',580,300,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Terbinafine 1% Cream 15g','Novartis','Skin Care','Allylamine antifungal for tinea pedis cruris and corporis.',280,400,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Mometasone 0.1% Cream 15g','MSD','Skin Care','Potent topical steroid for psoriasis eczema and contact dermatitis.',320,350,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Elocon 0.1% Ointment 15g','MSD','Skin Care','Mometasone furoate ointment for dry lichenified skin conditions.',380,300,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Calamine Lotion 100ml','Searle','Skin Care','Zinc oxide soothing lotion for insect bites prickly heat and rashes.',85,600,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Clobetasol 0.05% Cream 30g','GSK','Skin Care','Very potent steroid for refractory psoriasis and severe eczema.',290,280,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Silver Sulfadiazine Cream 50g','Sanofi','Skin Care','Antimicrobial cream for burn wound infection prevention.',480,200,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Benzoyl Peroxide 5% Gel 30g','Galderma','Skin Care','Topical acne treatment with antibacterial and keratolytic action.',320,350,'2028-04-01',false,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Tacrolimus 0.03% Ointment 30g','Astellas','Skin Care','Immunomodulator for atopic eczema when steroids are inappropriate.',2200,150,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Daktacort Cream 30g','J&J','Skin Care','Miconazole plus hydrocortisone for inflamed fungal skin infections.',260,320,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Psoriaid Lotion 100ml','Stiefel','Skin Care','Coal tar and salicylic acid lotion for scalp psoriasis.',780,200,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
-- ═══ VITAMINS (25) ═══
('Surbex Z','Abbott','Vitamins','B-complex vitamin C and zinc for energy and immunity.',420,500,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Cac 1000 Plus Effervescent','GSK','Vitamins','Calcium vitamin C and D3 effervescent tablet.',380,450,'2028-04-01',false,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Neurobion B1 B6 B12','Merck','Vitamins','Neurotropic vitamins for nerve function and fatigue.',260,800,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Centrum Silver 50+ Multivitamin','Pfizer','Vitamins','Complete multivitamin for mature adults 50 and over.',1850,150,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Vitamin D3 1000IU Softgels','Getz Pharma','Vitamins','Vitamin D3 supplement for bone health and immunity.',320,600,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Ferrous Sulphate 200mg Tablets','Searle','Vitamins','Oral iron supplement for iron-deficiency anaemia.',95,700,'2028-04-01',false,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Folic Acid 5mg Tablets','GSK','Vitamins','Folate supplement for pregnancy and megaloblastic anaemia.',45,900,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Calcium 600mg Plus D3 Tablets','Wyeth','Vitamins','Calcium and vitamin D combination for bone mineralisation.',380,500,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Zinc Sulphate 220mg Tablets','Abbott','Vitamins','Zinc supplement for immune support and wound healing.',180,500,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Vitamin C 1000mg Effervescent','Roche','Vitamins','High-dose vitamin C for antioxidant and immune support.',350,400,'2028-04-01',false,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Omega-3 Fish Oil 1000mg Softgels','Abbott','Vitamins','EPA and DHA for cardiovascular and cognitive health.',680,350,'2028-04-01',false,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Biotin 5mg Tablets','Pharmalab','Vitamins','Vitamin B7 for hair growth nail strength and skin health.',280,400,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Magnesium 375mg Tablets','Sanofi','Vitamins','Magnesium supplement for muscle function and energy metabolism.',320,350,'2028-04-01',false,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Vitamin B12 1000mcg Sublingual','Merck','Vitamins','Sublingual methylcobalamin for B12 deficiency and fatigue.',420,300,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Vitamin E 400IU Softgels','Pfizer','Vitamins','Antioxidant tocopherol for cardiovascular and skin health.',280,350,'2028-04-01',false,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Iron Folic Acid Syrup 200ml','Getz Pharma','Vitamins','Combined iron and folate liquid for pregnancy-related anaemia.',220,400,'2028-04-01',false,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Pregnacare Max Tablets','Vitabiotics','Vitamins','Complete prenatal multivitamin with omega-3 for pregnancy.',1850,200,'2028-04-01',false,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Ensure Plus Nutrition 400g','Abbott','Vitamins','Complete balanced nutrition powder for malnutrition recovery.',2400,250,'2028-04-01',false,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Coenzyme Q10 100mg Softgels','Pharmalab','Vitamins','CoQ10 ubiquinone for mitochondrial energy and heart health.',880,200,'2028-04-01',false,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Probiotics Lactobacillus 10B CFU','Sanofi','Vitamins','Multi-strain probiotic for gut microbiome and immune health.',680,280,'2028-04-01',false,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Vitamin K2 100mcg MK-7 Tablets','Pharmalab','Vitamins','Menaquinone-7 for calcium utilisation and bone health.',680,200,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Iodine 150mcg Tablets','Merck','Vitamins','Iodine supplement for thyroid hormone synthesis.',120,400,'2028-04-01',false,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Selenium 200mcg Tablets','Roche','Vitamins','Essential trace mineral for antioxidant defence and thyroid function.',380,250,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
-- ═══ ALLERGY (20) ═══
('Zyrtec 10mg Cetirizine','UCB','Allergy','Non-drowsy antihistamine for allergic rhinitis and urticaria.',220,700,'2028-04-01',false,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Claritin 10mg Loratadine','Bayer','Allergy','Once-daily non-sedating antihistamine for seasonal allergies.',280,600,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Allegra 120mg Fexofenadine','Sanofi','Allergy','Non-drowsy antihistamine for seasonal allergic rhinitis.',380,500,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Xyzal 5mg Levocetirizine','UCB','Allergy','Next-generation cetirizine for chronic urticaria and rhinitis.',320,450,'2028-04-01',false,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Polaramine 2mg Dexchlorpheniramine','MSD','Allergy','Classical antihistamine for acute allergic reactions.',95,600,'2028-04-01',false,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Phenergan 25mg Promethazine','Sanofi','Allergy','First-gen antihistamine for allergy nausea and premedication.',120,500,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Avil 45mg Pheniramine','Sanofi','Allergy','Antihistamine for allergy urticaria and insect sting reactions.',80,800,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Bilastine 20mg Tablets','Menarini','Allergy','New-generation antihistamine with rapid onset and long action.',380,300,'2028-04-01',false,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Rupatadine 10mg Tablets','Pfizer','Allergy','Dual-action antihistamine for urticaria and seasonal rhinitis.',420,250,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Desloratadine 5mg Tablets','MSD','Allergy','Active loratadine metabolite with extended antihistamine action.',350,400,'2028-04-01',false,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Atarax 25mg Hydroxyzine','UCB','Allergy','Antihistamine with anxiolytic effect for allergies and anxiety.',180,400,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Azelastine Nasal Spray 0.1%','Meda','Allergy','Topical antihistamine nasal spray for allergic rhinitis.',680,200,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Flonase Allergy Spray 120 doses','GSK','Allergy','Fluticasone propionate nasal spray for year-round allergies.',820,220,'2028-04-01',false,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Epipen 0.3mg Autoinjector','Pfizer','Allergy','Adrenaline autoinjector for anaphylaxis emergency treatment.',8500,80,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Pred Forte Eye Drops 1%','Allergan','Allergy','Prednisolone acetate for severe allergic conjunctivitis.',680,200,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Sodium Cromoglicate 4% Eye Drops','Sanofi','Allergy','Mast cell stabiliser for allergic and vernal conjunctivitis.',420,280,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Betnesol 0.5mg Betamethasone','GSK','Allergy','Systemic corticosteroid for severe allergic and inflammatory disorders.',320,350,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
-- ═══ NEUROLOGY (20) ═══
('Epilim 500mg Sodium Valproate','Sanofi','Neurology','Broad-spectrum anticonvulsant for epilepsy and bipolar disorder.',680,300,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Tegretol 200mg Carbamazepine','Novartis','Neurology','Anticonvulsant and mood stabiliser for epilepsy and trigeminal neuralgia.',280,400,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Lamotrigine 50mg Tablets','GSK','Neurology','Antiepileptic for focal seizures and bipolar depression.',520,280,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Phenobarbitone 30mg Tablets','Bayer','Neurology','Barbiturate anticonvulsant for tonic-clonic and focal seizures.',55,500,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Topiramate 100mg Tablets','J&J','Neurology','Anticonvulsant for epilepsy and migraine prophylaxis.',680,250,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Levetiracetam 500mg Tablets','UCB','Neurology','Novel anticonvulsant for generalised and focal epileptic seizures.',1200,200,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Sinemet 25/250 Levodopa+Carbidopa','MSD','Neurology','Gold-standard therapy for Parkinson disease motor symptoms.',980,180,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Pramipexole 1mg Tablets','Boehringer','Neurology','Dopamine agonist for Parkinson disease and restless legs syndrome.',1400,160,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Memantine 10mg Tablets','Merz','Neurology','NMDA antagonist for moderate to severe Alzheimer disease.',3200,100,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Donepezil 5mg Tablets','Pfizer','Neurology','Cholinesterase inhibitor for mild to moderate Alzheimer disease.',2600,140,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Tizanidine 2mg Tablets','Novartis','Neurology','Centrally-acting muscle relaxant for spasticity in MS and spinal cord.',380,300,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Baclofen 10mg Tablets','Novartis','Neurology','GABA-B agonist for spasticity in multiple sclerosis and spinal injury.',180,400,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Sumatriptan 50mg Tablets','GSK','Neurology','Triptan for acute migraine with or without aura.',980,200,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Zolmitriptan 2.5mg Tablets','AstraZeneca','Neurology','5-HT1B/1D agonist for acute treatment of migraine headaches.',1200,180,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Pizotifen 0.5mg Tablets','Novartis','Neurology','5-HT antagonist for prevention of migraine and cluster headaches.',180,350,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Propranolol 40mg Tablets','Astra','Neurology','Beta-blocker for migraine prophylaxis and essential tremor.',95,500,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Rasagiline 1mg Tablets','Teva','Neurology','MAO-B inhibitor for early Parkinson disease monotherapy.',2800,120,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Riluzole 50mg Tablets','Sanofi','Neurology','Neuroprotective for ALS amyotrophic lateral sclerosis treatment.',18000,40,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
-- ═══ PSYCHIATRY (20) ═══
('Prozac 20mg Fluoxetine','Eli Lilly','Psychiatry','SSRI for major depression OCD bulimia and panic disorder.',420,400,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Seroxat 20mg Paroxetine','GSK','Psychiatry','SSRI for depression anxiety disorders PTSD and OCD.',480,350,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Zoloft 50mg Sertraline','Pfizer','Psychiatry','SSRI for depression social anxiety PTSD and panic disorder.',560,350,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Lexapro 10mg Escitalopram','Lundbeck','Psychiatry','Selective SSRI for major depressive disorder and generalised anxiety.',620,320,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Effexor XR 75mg Venlafaxine','Pfizer','Psychiatry','SNRI for depression generalised anxiety and social phobia.',980,250,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Cymbalta 60mg Duloxetine','Eli Lilly','Psychiatry','SNRI for depression diabetic neuropathy and fibromyalgia.',1200,220,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Mirtazapine 30mg Tablets','Organon','Psychiatry','NaSSA antidepressant with sedative properties for depression.',580,280,'2028-04-01',true,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Lithium Carbonate 300mg Tablets','Pfizer','Psychiatry','Mood stabiliser for bipolar disorder mania and depression.',120,300,'2028-04-01',true,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Abilify 10mg Aripiprazole','Otsuka','Psychiatry','Atypical antipsychotic for schizophrenia and bipolar I disorder.',2800,150,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Olanzapine 10mg Tablets','Eli Lilly','Psychiatry','Atypical antipsychotic for schizophrenia and acute mania.',1850,180,'2028-04-01',true,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Risperidone 2mg Tablets','J&J','Psychiatry','Atypical antipsychotic for schizophrenia and aggression in autism.',680,250,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Quetiapine 25mg Tablets','AstraZeneca','Psychiatry','Atypical antipsychotic for schizophrenia bipolar and depression.',820,220,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW()),
('Clozapine 100mg Tablets','Novartis','Psychiatry','Atypical antipsychotic for treatment-resistant schizophrenia.',280,150,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Lorazepam 1mg Tablets','Wyeth','Psychiatry','Short-acting benzodiazepine for anxiety insomnia and status epilepticus.',95,400,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
('Diazepam 5mg Tablets','Roche','Psychiatry','Benzodiazepine for anxiety muscle spasm and alcohol withdrawal.',65,500,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Alprazolam 0.5mg Tablets','Pfizer','Psychiatry','Benzodiazepine for panic disorder and generalised anxiety.',120,450,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Clonazepam 0.5mg Tablets','Roche','Psychiatry','Benzodiazepine for panic disorder epilepsy and anxiety.',140,380,'2028-04-01',true,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Bupropion 150mg SR Tablets','GSK','Psychiatry','NDRI antidepressant and smoking cessation aid.',1800,180,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Trazodone 100mg Tablets','Bristol','Psychiatry','Serotonin antagonist and reuptake inhibitor for depression and insomnia.',480,250,'2028-04-01',true,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
-- ═══ EYE CARE (15) ═══
('Vigamox 0.5% Eye Drops 3ml','Alcon','Eye Care','Moxifloxacin antibiotic eye drops for bacterial conjunctivitis.',680,300,'2028-04-01',true,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Travatan 0.004% Eye Drops','Alcon','Eye Care','Travoprost prostaglandin for open-angle glaucoma and ocular hypertension.',1850,150,'2028-04-01',true,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Lumigan 0.01% Eye Drops','Allergan','Eye Care','Bimatoprost for lowering intraocular pressure in glaucoma.',1950,140,'2028-04-01',true,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW()),
('Timolol 0.5% Eye Drops 5ml','MSD','Eye Care','Beta-blocker eye drops reducing aqueous humour production in glaucoma.',480,250,'2028-04-01',true,true,'/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp',NOW()),
('Tobramycin 0.3% Eye Drops 5ml','Alcon','Eye Care','Aminoglycoside antibiotic for bacterial external eye infections.',320,280,'2028-04-01',true,true,'/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg',NOW()),
('Refresh Tears Eye Drops 15ml','Allergan','Eye Care','Preservative-free lubricant for dry eye syndrome.',420,350,'2028-04-01',false,true,'/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp',NOW()),
('Systane Ultra Eye Drops 10ml','Alcon','Eye Care','Polyethylene glycol lubricating drops for dry eye relief.',580,300,'2028-04-01',false,true,'/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp',NOW()),
('Voltaren Ophtha 0.1% Eye Drops','Novartis','Eye Care','Topical NSAID for ocular inflammation after eye surgery.',680,200,'2028-04-01',true,true,'/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg',NOW()),
('Hypromellose 0.3% Eye Drops','Bausch','Eye Care','Viscous tear substitute for moderate dry eye conditions.',280,400,'2028-04-01',false,true,'/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg',NOW()),
('Atropine 1% Eye Drops','Martindale','Eye Care','Cycloplegic for refraction uveitis and myopia management.',380,180,'2028-04-01',true,true,'/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg',NOW()),
('Latanoprost 0.005% Eye Drops','Pfizer','Eye Care','Prostaglandin analogue for primary open-angle glaucoma.',1650,160,'2028-04-01',true,true,'/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp',NOW())
ON CONFLICT DO NOTHING;

INSERT INTO "Medicines" ("Name","Brand","Category","Description","Price","StockQuantity","ExpiryDate","PrescriptionRequired","IsActive","ImageUrl","CreatedAt")
VALUES
('Olopatadine 0.1% Eye Drops 5ml','Alcon','Eye Care','Antihistamine and mast cell stabiliser for allergic conjunctivitis.',780,220,'2028-04-01',true,true,'/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp',NOW()),
('Dorzolamide 2% Eye Drops 5ml','MSD','Eye Care','Carbonic anhydrase inhibitor eye drops for raised IOP.',1200,180,'2028-04-01',true,true,'/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png',NOW()),
-- ═══ ENT (15) ═══
('Otosporin Ear Drops 5ml','GSK','ENT','Neomycin polymyxin hydrocortisone for otitis externa.',320,350,'2028-04-01',true,true,'/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg',NOW()),
('Sofradex Ear Eye Drops 8ml','Roussel','ENT','Framycetin gramicidin dexamethasone for ear and eye infections.',280,300,'2028-04-01',true,true,'/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp',NOW()),
('Xylometazoline 0.1% Nasal Drops','Novartis','ENT','Sympathomimetic nasal decongestant for acute nasal congestion.',180,500,'2028-04-01',false,true,'/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg',NOW()),
('Nasonex 50mcg Nasal Spray','MSD','ENT','Mometasone furoate for seasonal and perennial allergic rhinitis.',980,220,'2028-04-01',true,true,'/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp',NOW()),
('Strepsils Throat Lozenges','Reckitt','ENT','Antiseptic lozenges for sore throat and mild oral infections.',120,700,'2028-04-01',false,true,'/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg',NOW()),
('Betadine Gargle 250ml','Mundipharma','ENT','Povidone-iodine oral antiseptic for throat and oral infections.',280,400,'2028-04-01',false,true,'/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp',NOW()),
('Nasivion Mini 0.01% Drops','Merck','ENT','Oxymetazoline nasal drops for infants with nasal congestion.',220,400,'2028-04-01',false,true,'/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg',NOW()),
('Cerumol Ear Drops 11ml','Thornton Ross','ENT','Paradichlorobenzene drops for earwax softening and removal.',380,250,'2028-04-01',false,true,'/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG',NOW())
ON CONFLICT DO NOTHING;
