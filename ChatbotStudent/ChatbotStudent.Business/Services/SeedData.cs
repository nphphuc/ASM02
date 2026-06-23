using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatbotStudent.Business.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Seed Admin user if not exists
        var adminEmail = "admin@chatbot.edu";
        var adminUser = await userService.GetUserByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            await userService.CreateUserWithDetailsAsync(
                adminEmail, "admin123", "Quản trị viên", UserRole.Admin,
                "Đại học Công nghệ, ĐHQGHN", null, null, null);
            // Admin is auto-approved by CreateUserWithDetailsAsync
        }
        else
        {
            // Ensure existing seed users have correct data
            adminUser.ApprovalStatus = ApprovalStatus.Approved;
            if (string.IsNullOrEmpty(adminUser.UniversityName))
                adminUser.UniversityName = "Đại học Công nghệ, ĐHQGHN";
            await userManager.UpdateAsync(adminUser);
        }

        // Seed demo Lecturer (auto-approved by CreateUserWithDetailsAsync)
        var lecturerEmail = "giangvien@chatbot.edu";
        var lecturerUser = await userService.GetUserByEmailAsync(lecturerEmail);
        if (lecturerUser == null)
        {
            await userService.CreateUserWithDetailsAsync(
                lecturerEmail, "lecturer123", "Giảng viên AI", UserRole.Lecturer,
                "Đại học Công nghệ, ĐHQGHN", null, "GV.2021001", "Phó giáo sư");
        }
        else
        {
            lecturerUser.ApprovalStatus = ApprovalStatus.Approved;
            if (string.IsNullOrEmpty(lecturerUser.UniversityName))
                lecturerUser.UniversityName = "Đại học Công nghệ, ĐHQGHN";
            if (string.IsNullOrEmpty(lecturerUser.LecturerCode))
                lecturerUser.LecturerCode = "GV.2021001";
            if (string.IsNullOrEmpty(lecturerUser.Title))
                lecturerUser.Title = "Phó giáo sư";
            await userManager.UpdateAsync(lecturerUser);
        }

        // Seed demo Student (auto-approved by CreateUserWithDetailsAsync)
        var studentEmail = "sinhvien@chatbot.edu";
        var studentUser = await userService.GetUserByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            await userService.CreateUserWithDetailsAsync(
                studentEmail, "student123", "Sinh viên AI", UserRole.Student,
                "Đại học Công nghệ, ĐHQGHN", "2202xxxx", null, null);
        }
        else
        {
            studentUser.ApprovalStatus = ApprovalStatus.Approved;
            if (string.IsNullOrEmpty(studentUser.UniversityName))
                studentUser.UniversityName = "Đại học Công nghệ, ĐHQGHN";
            if (string.IsNullOrEmpty(studentUser.StudentCode))
                studentUser.StudentCode = "2202xxxx";
            await userManager.UpdateAsync(studentUser);
        }

        // Only seed courses if database is empty
        if (await db.Courses.AnyAsync())
            return;

        // Create a demo course
        var course = new Course
        {
            Name = "Trí tuệ nhân tạo",
            Code = "AI101",
            Description = "Môn học giới thiệu về Trí tuệ nhân tạo, bao gồm các thuật toán tìm kiếm, học máy, học sâu, và ứng dụng AI trong thực tế.",
            CreatedAt = DateTime.UtcNow,
            LecturerId = (await userService.GetUserByEmailAsync(lecturerEmail))?.Id
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        // Enroll demo student in course
        if (studentUser != null)
        {
            await userService.EnrollStudentInCourseAsync(studentUser.Id, course.Id);
        }

        // Seed SystemConfig defaults
        if (!await db.SystemConfigs.AnyAsync())
        {
            foreach (var (key, defaultValue, description, category) in SystemConfigDefaults.GetDefaults())
            {
                db.SystemConfigs.Add(new SystemConfig
                {
                    Key = key,
                    Value = defaultValue,
                    Description = description,
                    Category = category,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        // Seed benchmark questions (50 câu hỏi + ground truth)
        var questions = GetSeedQuestions(course.Id);
        db.BenchmarkQuestions.AddRange(questions);
        await db.SaveChangesAsync();
    }

    private static List<BenchmarkQuestion> GetSeedQuestions(int courseId)
    {
        return new List<BenchmarkQuestion>
        {
            // Nhóm câu hỏi cơ bản
            new BenchmarkQuestion { CourseId = courseId, Question = "Trí tuệ nhân tạo (AI) là gì?", GroundTruth = "Trí tuệ nhân tạo (AI) là một nhánh của khoa học máy tính tập trung vào việc tạo ra các hệ thống có thể thực hiện các nhiệm vụ thường đòi hỏi trí thông minh của con người, như nhận dạng hình ảnh, dịch ngôn ngữ, ra quyết định và học hỏi từ kinh nghiệm.", Category = "Cơ bản", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Phân biệt AI yếu (Narrow AI) và AI mạnh (General AI)", GroundTruth = "AI yếu (Narrow AI/Weak AI) là hệ thống được thiết kế để thực hiện một nhiệm vụ cụ thể như nhận dạng giọng nói hay chơi cờ. AI mạnh (General AI/Strong AI) là hệ thống có khả năng thực hiện bất kỳ nhiệm vụ trí tuệ nào mà con người có thể làm, với khả năng tư duy trừu tượng và học hỏi chung.", Category = "Cơ bản", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Các trường phái chính trong nghiên cứu AI là gì?", GroundTruth = "Các trường phái chính bao gồm: (1) Symbolic AI - sử dụng biểu tượng và quy tắc logic, (2) Machine Learning - học từ dữ liệu, (3) Deep Learning - sử dụng mạng nơ-ron sâu, (4) Statistical AI - dựa trên thống kê, (5) Evolutionary Computing - mô phỏng quá trình tiến hóa.", Category = "Cơ bản", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "AI hoạt động như thế nào trong đời sống hàng ngày?", GroundTruth = "AI hoạt động trong nhiều ứng dụng hàng ngày: trợ lý ảo (Siri, Alexa), hệ thống gợi ý (Netflix, Spotify), nhận dạng khuôn mặt trên điện thoại, dịch thuật tự động (Google Translate), xe tự hành, chatbot hỗ trợ khách hàng, lọc spam email, và chẩn đoán y tế.", Category = "Ứng dụng", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Định nghĩa thuật ngữ 'machine learning' là gì?", GroundTruth = "Machine Learning (Học máy) là một nhánh của AI cho phép máy tính học hỏi và cải thiện từ kinh nghiệm mà không cần được lập trình rõ ràng. Nó hoạt động bằng cách tìm kiếm các mẫu trong dữ liệu và sử dụng các mẫu đó để đưa ra quyết định hoặc dự đoán trong tương lai.", Category = "Cơ bản", Difficulty = "Dễ" },

            // Nhóm câu hỏi về Search Algorithm
            new BenchmarkQuestion { CourseId = courseId, Question = "Giải thích thuật toán BFS (Breadth-First Search) và ứng dụng của nó.", GroundTruth = "BFS là thuật toán tìm kiếm theo chiều rộng, duyệt qua tất cả các nút ở mức hiện tại trước khi chuyển sang mức tiếp theo. BFS sử dụng hàng đợi (queue) để quản lý các nút cần duyệt. Ứng dụng: tìm đường ngắn nhất trong đồ thị không trọng số, tìm kiếm trong cây, kiểm tra连通性 của đồ thị.", Category = "Thuật toán tìm kiếm", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "So sánh thuật toán DFS và BFS trong tìm kiếm.", GroundTruth = "DFS (Depth-First Search) duyệt theo chiều sâu, đi hết một nhánh trước khi quay lại. BFS duyệt theo chiều rộng, duyệt hết một tầng trước khi xuống tầng tiếp. DFS sử dụng ngăn xếp (stack), BFS sử dụng hàng đợi (queue). BFS tìm đường ngắn nhất, DFS tiết kiệm bộ nhớ hơn. DFS phù hợp cho bài toán khám phá, BFS phù hợp cho bài toán tìm đường.", Category = "Thuật toán tìm kiếm", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Thuật toán A* hoạt động như thế nào?", GroundTruth = "A* là thuật toán tìm kiếm không informed sử dụng hàm đánh giá f(n) = g(n) + h(n), trong đó g(n) là chi phí từ điểm bắt đầu đến nút hiện tại, h(n) là hàm heuristic ước lượng chi phí từ nút hiện tại đến đích. A* đảm bảo tìm được lời giải tối ưu nếu hàm heuristic là admissible (không bao giờ overestimate).", Category = "Thuật toán tìm kiếm", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Hàm heuristic trong tìm kiếm là gì và tại sao nó quan trọng?", GroundTruth = "Hàm heuristic là hàm ước lượng chi phí từ trạng thái hiện tại đến mục tiêu. Nó quan trọng vì giúp hướng dẫn tìm kiếm hiệu quả hơn bằng cách ưu tiên khám phá các trạng thái có tiềm năng tốt hơn. Các hàm heuristic phổ biến bao gồm khoảng cách Euclidean, Manhattan, và khoảng cách Hamming.", Category = "Thuật toán tìm kiếm", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Giải thích thuật toán Greedy Best-First Search.", GroundTruth = "Greedy Best-First Search là thuật toán tìm kiếm luôn chọn nút có giá trị heuristic h(n) nhỏ nhất để mở rộng tiếp theo. Nó không xem xét chi phí thực tế từ điểm bắt đầu (g(n)). Thuật toán này nhanh nhưng không đảm bảo tìm được lời giải tối ưu vì nó chỉ quan tâm đến khoảng cách ước lượng đến mục tiêu.", Category = "Thuật toán tìm kiếm", Difficulty = "Trung bình" },

            // Nhóm câu hỏi về Machine Learning
            new BenchmarkQuestion { CourseId = courseId, Question = "Phân loại các loại học máy chính là gì?", GroundTruth = "Các loại học máy chính bao gồm: (1) Supervised Learning (Học có giám sát) - học từ dữ liệu đã được gắn nhãn, (2) Unsupervised Learning (Học không giám sát) - tìm cấu trúc trong dữ liệu không có nhãn, (3) Reinforcement Learning (Học tăng cường) - học thông qua tương tác với môi trường và phần thưởng.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Giải thích về overfitting trong machine learning và cách phòng tránh.", GroundTruth = "Overfitting là hiện tượng mô hình học quá tốt trên dữ liệu huấn luyện nhưng hoạt động kém trên dữ liệu mới. Nguyên nhân: mô hình quá phức tạp, dữ liệu huấn luyện quá ít, hoặc huấn luyện quá lâu. Cách phòng tránh: regularization (L1, L2), dropout, early stopping, cross-validation, tăng dữ liệu huấn luyện (data augmentation), giảm độ phức tạp mô hình.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Thuật toán Decision Tree hoạt động như thế nào?", GroundTruth = "Decision Tree (Cây quyết định) là mô hình học có giám sát sử dụng cấu trúc cây để đưa ra quyết định. Mỗi nút trong là một kiểm tra điều kiện, mỗi nhánh là một kết quả của kiểm tra, và mỗi lá là một nhãn đầu ra. Tree được xây dựng bằng cách chọn đặc trưng tốt nhất để phân tách dữ liệu tại mỗi nút, sử dụng các tiêu chí như Information Gain hoặc Gini Index.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Random Forest là gì và tại sao nó hiệu quả?", GroundTruth = "Random Forest là phương pháp ensemble learning kết hợp nhiều Decision Tree. Nó xây dựng nhiều cây quyết định trên các tập con ngẫu nhiên của dữ liệu và các đặc trưng, sau đó tổng hợp kết quả (bỏ phiếu cho phân loại hoặc trung bình cho hồi quy). Random Forest hiệu quả vì giảm variance, chống overfitting, và cải thiện độ chính xác so với单一的决策树.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Cross-validation hoạt động như thế nào?", GroundTruth = "Cross-validation là kỹ thuật đánh giá mô hình bằng cách chia dữ liệu thành k phần (folds). Quá trình huấn luyện và đánh giá lặp lại k lần, mỗi lần sử dụng một phần làm dữ liệu kiểm tra và k-1 phần còn lại làm dữ liệu huấn luyện. Kết quả cuối cùng là trung bình của k lần đánh giá. K-fold cross-validation giúp ước lượng hiệu suất mô hình một cách đáng tin cậy hơn.", Category = "Học máy", Difficulty = "Trung bình" },

            // Nhóm câu hỏi về Deep Learning
            new BenchmarkQuestion { CourseId = courseId, Question = "Mạng nơ-ron nhân tạo (ANN) hoạt động như thế nào?", GroundTruth = "ANN mô phỏng cấu trúc não bộ với các nút (neuron) được tổ chức thành các lớp: input, hidden, và output. Mỗi nút nhận đầu vào, áp dụng trọng số và hàm kích hoạt (ReLU, Sigmoid, Tanh), rồi truyền kết quả đến lớp tiếp theo. Mạng học bằng cách điều chỉnh trọng số thông qua backpropagation và gradient descent để giảm thiểu hàm mất mát.", Category = "Học sâu", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Giải thích về CNN (Convolutional Neural Network).", GroundTruth = "CNN là mạng nơ-ron chuyên xử lý dữ liệu dạng lưới như hình ảnh. Các thành phần chính: (1) Convolutional layer - sử dụng bộ lọc để trích xuất đặc trưng, (2) Pooling layer - giảm kích thước bản đồ đặc trưng, (3) Fully connected layer - phân loại dựa trên các đặc trưng đã trích xuất. CNN hiệu quả trong nhận dạng hình ảnh, phân loại ảnh, và phát hiện đối tượng.", Category = "Học sâu", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "RNN và LSTM khác nhau như thế nào?", GroundTruth = "RNN (Recurrent Neural Network) xử lý dữ liệu tuần tự bằng cách duy trì trạng thái ẩn từ các bước thời gian trước. Tuy nhiên, RNN gặp vấn đề vanishing gradient với dữ liệu dài. LSTM (Long Short-Term Memory) cải tiến RNN bằng cách thêm cơ chế cổng (gate) bao gồm: forget gate, input gate, và output gate, giúp mạng ghi nhớ thông tin dài hạn hiệu quả hơn.", Category = "Học sâu", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Transformer hoạt động như thế nào trong xử lý ngôn ngữ tự nhiên?", GroundTruth = "Transformer sử dụng cơ chế Self-Attention để xử lý toàn bộ chuỗi đầu vào đồng thời thay vì tuần tự. Các thành phần chính: (1) Multi-Head Attention - cho phép mô hình tập trung vào nhiều vị trí khác nhau, (2) Positional Encoding - thêm thông tin vị trí, (3) Feed-Forward Network - xử lý sau attention. Transformer là nền tảng của BERT, GPT, và các mô hình ngôn ngữ lớn hiện đại.", Category = "Học sâu", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Transfer Learning trong deep learning là gì?", GroundTruth = "Transfer Learning là kỹ thuật sử dụng mô hình đã được huấn luyện trên một bài toán lớn làm điểm khởi đầu cho bài toán mới. Thay vì huấn luyện từ đầu, ta fine-tune các lớp cuối của mô hình trên dữ liệu mới. Ưu điểm: tiết kiệm thời gian và dữ liệu, hiệu quả tốt hơn khi dữ liệu mới hạn chế. Ví dụ: sử dụng mô hình huấn luyện trên ImageNet cho bài toán phân loại y tế.", Category = "Học sâu", Difficulty = "Trung bình" },

            // Nhóm câu hỏi về NLP
            new BenchmarkQuestion { CourseId = courseId, Question = "Xử lý ngôn ngữ tự nhiên (NLP) bao gồm những task nào?", GroundTruth = "Các task chính trong NLP bao gồm: (1) Phân loại văn bản, (2) Phân tích cảm xúc, (3) Nhận dạng thực thể có tên (NER), (4) Tóm tắt văn bản, (5) Dịch máy, (6) Trả lời câu hỏi, (7) Phân tích cú pháp, (8) Tạo văn bản, (9) Phân đoạn từ, (10) Gắn nhãn POS (Part-of-Speech tagging).", Category = "Xử lý ngôn ngữ", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Word Embedding là gì và các phương pháp phổ biến?", GroundTruth = "Word Embedding là kỹ thuật biểu diễn từ dạng vector số trong không gian đa chiều, trong đó các từ có ý nghĩa tương tự có vector gần nhau. Các phương pháp phổ biến: (1) Word2Vec (CBOW, Skip-gram), (2) GloVe - sử dụng ma trận đồng xuất hiện, (3) FastText - sử dụng subword. Modern approaches: BERT embedding, Sentence-BERT embedding.", Category = "Xử lý ngôn ngữ", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Giải thích về tokenization trong NLP.", GroundTruth = "Tokenization là quá trình chia văn bản thành các đơn vị nhỏ hơn (tokens) để xử lý. Các loại tokenization: (1) Word-level - chia theo từ, (2) Character-level - chia theo ký tự, (3) Subword-level (BPE, WordPiece) - chia thành các phần con của từ. Subword tokenization được sử dụng rộng rãi trong các mô hình transformer hiện đại vì cân bằng giữa hiệu quả và khả năng xử lý từ mới.", Category = "Xử lý ngôn ngữ", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Bag of Words (BoW) và TF-IDF khác nhau thế nào?", GroundTruth = "Bag of Words (BoW) biểu diễn văn bản bằng cách đếm tần suất xuất hiện của mỗi từ, không quan tâm thứ tự. TF-IDF cải tiến BoW bằng cách tính trọng số: TF (Term Frequency) - tần suất từ trong tài liệu, IDF (Inverse Document Frequency) - giảm trọng số cho từ xuất hiện ở nhiều tài liệu. TF-IDF giúp làm nổi bật các từ quan trọng và phân biệt giữa các tài liệu.", Category = "Xử lý ngôn ngữ", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "BERT hoạt động như thế nào?", GroundTruth = "BERT (Bidirectional Encoder Representations from Transformers) là mô hình language pretrained sử dụng cơ chế Masked Language Model (MLM) và Next Sentence Prediction (NSP). MLM: masks ngẫu nhiên 15% tokens và dự đoán chúng. NSP: dự đoán câu tiếp theo có liền kề không. BERT có khả năng hiểu ngữ cảnh hai chiều (bidirectional), cho phép hiểu ngữ nghĩa sâu sắc hơn so với các mô hình unidirectional trước đó.", Category = "Xử lý ngôn ngữ", Difficulty = "Khó" },

            // Nhóm câu hỏi về RAG
            new BenchmarkQuestion { CourseId = courseId, Question = "RAG (Retrieval Augmented Generation) là gì?", GroundTruth = "RAG là kỹ thuật kết hợp retriever (truy xuất thông tin) và generator (tạo văn bản) để cải thiện câu trả lời của mô hình ngôn ngữ. Quy trình: (1) Truy xuất các tài liệu liên quan từ cơ sở dữ liệu, (2) Cung cấp ngữ cảnh cho LLM, (3) LLM tạo câu trả lời dựa trên ngữ cảnh. RAG giúp giảm hallucination và cập nhật kiến thức mà không cần fine-tune lại mô hình.", Category = "RAG", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Các bước trong pipeline RAG là gì?", GroundTruth = "Pipeline RAG gồm: (1) Document Processing - xử lý tài liệu, trích xuất text, (2) Chunking - chia tài liệu thành các đoạn nhỏ, (3) Embedding - chuyển text thành vector, (4) Indexing - lưu vectors vào cơ sở dữ liệu, (5) Retrieval - tìm các chunks liên quan khi có query, (6) Generation - LLM tạo câu trả lời dựa trên chunks retrieved, (7) Post-processing - định dạng và kiểm tra câu trả lời.", Category = "RAG", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Chunking strategy nào hiệu quả nhất cho RAG?", GroundTruth = "Không có chunking strategy tốt nhất cho mọi trường hợp, nhưng: (1) Fixed-size đơn giản nhưng có thể cắt ngang câu, (2) Sentence-based giữ nguyên câu nhưng có thể vượt quá giới hạn token, (3) Paragraph-based giữ nguyên ý nghĩa nhưng kích thước không đồng đều, (4) Recursive balanced giữa các yếu tố. Strategy hiệu quả nhất phụ thuộc vào loại tài liệu và mô hình embedding được sử dụng.", Category = "RAG", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Embedding models nào phù hợp cho RAG tiếng Việt?", GroundTruth = "Các embedding models phù hợp cho RAG tiếng Việt: (1) multilingual-e5-base - miễn phí, hỗ trợ đa ngôn ngữ tốt, (2) text-embedding-3-small (OpenAI) - chất lượng cao, có phí, (3) PhoBERT-base - được huấn luyện riêng cho tiếng Việt, (4) bge-m3 (BAAI) - hiệu suất tốt cho đa ngôn ngữ. Lựa chọn phụ thuộc vào yêu cầu về chất lượng, chi phí, và tốc độ.", Category = "RAG", Difficulty = "Trung bình" },

            // Nhóm câu hỏi về RAGAS Benchmark
            new BenchmarkQuestion { CourseId = courseId, Question = "RAGAS là gì và các metric chính của nó?", GroundTruth = "RAGAS (RAG Assessment) là framework đánh giá hệ thống RAG. Các metric chính: (1) Faithfulness - câu trả lời có được hỗ trợ bởi ngữ cảnh không, (2) Answer Relevance - câu trả lời có liên quan đến câu hỏi không, (3) Context Precision - ngữ cảnh truy xuất có chính xác không, (4) Context Recall - ngữ cảnh có đầy đủ thông tin không. RAGAS sử dụng LLM làm judge để đánh giá.", Category = "Đánh giá", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Faithfulness metric trong RAGAS đo lường điều gì?", GroundTruth = "Faithfulness đo lường mức độ mà câu trả lời được tạo ra dựa trên ngữ cảnh đã truy xuất. Faithfulness = 1.0 nghĩa là mọi tuyên bố trong câu trả lời đều có thể được tìm thấy trong ngữ cảnh. Faithfulness thấp cho thấy mô hình đang hallucinate hoặc đưa ra thông tin không có trong tài liệu nguồn.", Category = "Đánh giá", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Tại sao cần benchmark RAG và làm thế nào để thực hiện?", GroundTruth = "Benchmark RAG cần thiết để: (1) So sánh hiệu quả các cấu hình khác nhau, (2) Đánh giá chất lượng câu trả lời, (3) Tối ưu hóa tham số, (4) Theo dõi cải tiến theo thời gian. Cách thực hiện: (1) Tạo test set với ground truth, (2) Chạy experiments với các biến số khác nhau (embedding model, chunking strategy), (3) Đánh giá bằng RAGAS metrics, (4) Phân tích kết quả và chọn cấu hình tốt nhất.", Category = "Đánh giá", Difficulty = "Trung bình" },

            // Nhóm câu hỏi nâng cao
            new BenchmarkQuestion { CourseId = courseId, Question = "Fine-tuning là gì và so sánh với RAG?", GroundTruth = "Fine-tuning là quá trình huấn luyện thêm mô hình language model trên dữ liệu cụ thể để nó thích ứng với tác vụ cụ thể. So sánh với RAG: (1) Fine-tuning thay đổi trọng số mô hình, RAG giữ nguyên mô hình, (2) Fine-tuning cần nhiều dữ liệu huấn luyện, RAG cần tài liệu tham khảo, (3) Fine-tuning chi phí cao hơn, RAG linh hoạt hơn với dữ liệu mới, (4) RAG tốt cho Q&A dựa trên tài liệu, Fine-tuning tốt cho thay đổi hành vi mô hình.", Category = "Nâng cao", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Hallucination trong AI là gì và cách giảm thiểu?", GroundTruth = "Hallucination là hiện tượng mô hình AI tạo ra thông tin sai lệch hoặc không có thật nhưng trình bày rất tự tin. Cách giảm thiểu: (1) RAG - cung cấp tài liệu tham khảo, (2) Prompt engineering - hướng dẫn mô hình không tạo thông tin mới, (3) Fine-tuning với dữ liệu chất lượng cao, (4) Kiểm tra chéo với nguồn tin, (5) Temperature thấp hơn, (6) Structured output với facts checking.", Category = "Nâng cao", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Cosine Similarity hoạt động như thế nào trong vector search?", GroundTruth = "Cosine Similarity đo lường góc giữa hai vector trong không gian đa chiều, giá trị từ -1 đến 1. Công thức: cos(θ) = (A·B) / (|A|×|B|). Trong vector search: (1) Query được chuyển thành vector embedding, (2) So sánh cosine similarity với tất cả vectors trong index, (3) Trả về top-K vectors có similarity cao nhất. Cosine similarity tốt hơn Euclidean distance vì không bị ảnh hưởng bởi độ dài vector.", Category = "Nâng cao", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Vector database là gì và tại sao cần nó cho RAG?", GroundTruth = "Vector database là cơ sở dữ liệu chuyên lưu trữ và tìm kiếm vectors (embeddings). Nó cần cho RAG vì: (1) Lưu trữ embeddings của chunks tài liệu, (2) Hỗ trợ approximate nearest neighbor (ANN) search nhanh, (3) Scalable với hàng triệu vectors. Các vector DB phổ biến: Pinecone, Weaviate, Milvus, ChromaDB. SQL Server 2025 cũng hỗ trợ vector search với DiskANN.", Category = "Nâng cao", Difficulty = "Khó" },

            // Nhóm câu hỏi về Evaluation & Ethics
            new BenchmarkQuestion { CourseId = courseId, Question = "Precision và Recall trong AI đánh giá điều gì?", GroundTruth = "Precision (Độ chính xác) = TP/(TP+FP) - tỷ lệ dự đoán dương tính thực sự đúng. Recall (Độ gợi lại) = TP/(TP+FN) - tỷ lệ mẫu dương tính được phát hiện đúng. F1-score = harmonic mean của Precision và Recall. Precision quan trọng khi FP costly (spam filter), Recall quan trọng khi FN costly (chẩn đoán bệnh).", Category = "Đánh giá", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Confusion Matrix là gì?", GroundTruth = "Confusion Matrix (Ma trận nhầm lẫn) là bảng 2x2 thể hiện hiệu suất mô hình phân loại: True Positive (TP), True Negative (TN), False Positive (FP), False Negative (FN). Từ đó tính được: Accuracy = (TP+TN)/(TP+TN+FP+FN), Precision, Recall, F1-score. Confusion Matrix giúp hiểu rõ loại lỗi mà mô hình mắc phải.", Category = "Đánh giá", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Ethical AI là gì và các vấn đề đạo đức chính?", GroundTruth = "Ethical AI đề cập đến các nguyên tắc và thực hành đạo đức trong phát triển và sử dụng AI. Các vấn đề chính: (1) Bias - thiên kiến trong dữ liệu và mô hình, (2) Transparency - minh bạch trong ra quyết định, (3) Privacy - bảo mật dữ liệu cá nhân, (4) Accountability - trách nhiệm giải trình, (5) Fairness - công bằng trong đối xử, (6) Safety - an toàn khi sử dụng AI.", Category = "Đạo đức AI", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Prompt Engineering là gì và các kỹ thuật cơ bản?", GroundTruth = "Prompt Engineering là kỹ thuật thiết kế prompt (lời nhắc) hiệu quả để hướng dẫn LLM tạo ra kết quả tốt. Các kỹ thuật cơ bản: (1) Zero-shot - hỏi trực tiếp không có ví dụ, (2) Few-shot - cung cấp một số ví dụ, (3) Chain-of-Thought - hướng dẫn tư duy từng bước, (4) Role-playing - gán vai trò cho LLM, (5) Structured Output - yêu cầu định dạng cụ thể.", Category = "Nâng cao", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Ensemble Learning là gì và các phương pháp phổ biến?", GroundTruth = "Ensemble Learning kết hợp nhiều mô hình để tạo ra dự đoán tốt hơn. Các phương pháp: (1) Bagging - huấn luyện nhiều mô hình trên dữ liệu bootstrap (Random Forest), (2) Boosting - huấn luyện tuần tự, mỗi mô hình sửa lỗi của mô hình trước (XGBoost, AdaBoost), (3) Stacking - sử dụng meta-learner để kết hợp dự đoán, (4) Voting - bỏ phiếu đa số.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Gradient Descent hoạt động như thế nào?", GroundTruth = "Gradient Descent là thuật toán tối ưu hóa tìm cực trị của hàm mất mát. Quy trình: (1) Khởi tạo trọng số ngẫu nhiên, (2) Tính gradient (đạo hàm) của hàm mất mát, (3) Cập nhật trọng số: w = w - learning_rate × gradient, (4) Lặp lại cho đến khi hội tụ. Variants: Batch GD (toàn bộ dữ liệu), Stochastic GD (mẫu đơn), Mini-batch GD (nhóm nhỏ).", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Normalization và Standardization khác nhau thế nào?", GroundTruth = "Normalization (Min-Max Scaling): chuyển dữ liệu về khoảng [0,1] bằng công thức (x - min)/(max - min). Phù hợp khi dữ liệu không có outlier. Standardization (Z-score): chuyển dữ liệu về phân phối chuẩn với mean=0, std=1 bằng công thức (x - mean)/std. Phù hợp khi dữ liệu có outlier. Cả hai đều cần thiết để các thuật toán ML hoạt động tốt hơn.", Category = "Học máy", Difficulty = "Dễ" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Feature Selection và Feature Extraction khác nhau thế nào?", GroundTruth = "Feature Selection: chọn lọc đặc trưng từ tập đặc trưng hiện có mà không thay đổi chúng. Phương pháp: Filter (chi-square, correlation), Wrapper (forward/backward selection), Embedded (LASSO). Feature Extraction: tạo đặc trưng mới từ đặc trưng hiện có. Phương pháp: PCA (Principal Component Analysis), LDA (Linear Discriminant Analysis), Autoencoders. Cả hai đều giúp giảm chiều dữ liệu.", Category = "Học máy", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Reinforcement Learning hoạt động như thế nào?", GroundTruth = "Reinforcement Learning: agent học từ môi trường bằng cách thực hiện hành động và nhận phần thưởng/penalty. Các thành phần: State (trạng thái), Action (hành động), Reward (phần thưởng), Policy (chính sách). Thuật toán: Q-Learning (model-free), SARSA, Deep Q-Network (DQN), Policy Gradient. Ứng dụng: chơi game, robot, tự lái xe, tối ưu hóa hệ thống.", Category = "Học máy", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "GAN (Generative Adversarial Network) là gì?", GroundTruth = "GAN gồm hai mạng: Generator (tạo dữ liệu giả) và Discriminator (phân biệt thật/giả). Hai mạng cạnh tranh: Generator cố tạo dữ liệu giống thật, Discriminator cố phát hiện giả. Quá trình training cho đến khi Discriminator không phân biệt được. Ứng dụng: tạo ảnh, style transfer, super-resolution, data augmentation.", Category = "Học sâu", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Attention Mechanism trong Deep Learning là gì?", GroundTruth = "Attention Mechanism cho phép mô hình tập trung vào các phần quan trọng của dữ liệu đầu vào khi tạo đầu ra. Self-Attention: mỗi phần tử trong chuỗi attend vào tất cả các phần tử khác. Multi-Head Attention: chạy nhiều attention song song để捕捉 các mối quan hệ khác nhau. Attention là thành phần cốt lõi của Transformer, giúp xử lý chuỗi dài hiệu quả hơn RNN.", Category = "Học sâu", Difficulty = "Khó" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Batch Normalization có tác dụng gì?", GroundTruth = "Batch Normalization chuẩn hóa đầu ra của layer trước khi truyền vào layer tiếp theo, giúp: (1) Giảm Internal Covariate Shift, (2) Tốc độ huấn luyện nhanh hơn, (3) Cho phép learning rate cao hơn, (4) Giảm phụ thuộc vào initialization, (5) Có tác dụng regularization nhẹ. Tính mean và variance trên mỗi mini-batch trong training, và sử dụng running statistics trong inference.", Category = "Học sâu", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Object Detection trong Computer Vision là gì?", GroundTruth = "Object Detection xác định vị trí và loại của các đối tượng trong ảnh bằng bounding box + class label. Các mô hình phổ biến: (1) Two-stage: R-CNN, Fast R-CNN, Faster R-CNN, (2) One-stage: YOLO, SSD, (3) Anchor-free: CornerNet, FCOS. Metrics: IoU (Intersection over Union), mAP (mean Average Precision). Ứng dụng: tự lái xe, giám sát, y tế.", Category = "Computer Vision", Difficulty = "Trung bình" },
            new BenchmarkQuestion { CourseId = courseId, Question = "Semantic Segmentation khác với Object Detection thế nào?", GroundTruth = "Object Detection: xác định bounding box + class label cho mỗi đối tượng. Semantic Segmentation: phân loại từng pixel trong ảnh thành một class cụ thể, tạo ra bản đồ phân đoạn pixel-level. Instance Segmentation: kết hợp cả hai - phân loại pixel và phân biệt giữa các instance khác nhau cùng class. Ứng dụng: lái xe tự hành (nhận diện đường, vạch kẻ đường).", Category = "Computer Vision", Difficulty = "Khó" },
        };
    }
}
