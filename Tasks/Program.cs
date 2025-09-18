using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Enums for exam states and question types
public enum ExamMode { Starting, Queued, Finished }
public enum QuestionType { TrueFalse, ChooseOne, ChooseAll }

// Base Question class
public abstract class Question : ICloneable, IComparable<Question>
{
    public string Header { get; set; }
    public string Body { get; set; }
    public int Marks { get; set; }
    public QuestionType Type { get; protected set; }

    protected Question(string header, string body, int marks)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Marks = marks > 0 ? marks : throw new ArgumentException("Marks must be positive");
    }

    public abstract void Display();
    public abstract object Clone();

    public virtual int CompareTo(Question other)
    {
        if (other == null) return 1;
        return Marks.CompareTo(other.Marks);
    }

    public override bool Equals(object obj)
    {
        if (obj is Question other)
            return Header == other.Header && Body == other.Body;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Header, Body);
    }

    public override string ToString()
    {
        return $"[{Type}] {Header}: {Body} ({Marks} marks)";
    }
}

// True/False Question
public class TrueFalseQuestion : Question
{
    public bool CorrectAnswer { get; set; }

    public TrueFalseQuestion(string header, string body, int marks, bool correctAnswer)
        : base(header, body, marks)
    {
        Type = QuestionType.TrueFalse;
        CorrectAnswer = correctAnswer;
    }

    public override void Display()
    {
        Console.WriteLine($"{Header}");
        Console.WriteLine($"{Body}");
        Console.WriteLine("a) True");
        Console.WriteLine("b) False");
        Console.WriteLine($"Marks: {Marks}");
    }

    public override object Clone()
    {
        return new TrueFalseQuestion(Header, Body, Marks, CorrectAnswer);
    }
}

// Choose One Question
public class ChooseOneQuestion : Question
{
    public List<string> Options { get; set; }
    public int CorrectAnswerIndex { get; set; }

    public ChooseOneQuestion(string header, string body, int marks, List<string> options, int correctAnswerIndex)
        : base(header, body, marks)
    {
        Type = QuestionType.ChooseOne;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        CorrectAnswerIndex = correctAnswerIndex;
    }

    public override void Display()
    {
        Console.WriteLine($"{Header}");
        Console.WriteLine($"{Body}");
        for (int i = 0; i < Options.Count; i++)
        {
            Console.WriteLine($"{(char)('a' + i)}) {Options[i]}");
        }
        Console.WriteLine($"Marks: {Marks}");
    }

    public override object Clone()
    {
        return new ChooseOneQuestion(Header, Body, Marks, new List<string>(Options), CorrectAnswerIndex);
    }
}

// Choose All Question
public class ChooseAllQuestion : Question
{
    public List<string> Options { get; set; }
    public List<int> CorrectAnswerIndexes { get; set; }

    public ChooseAllQuestion(string header, string body, int marks, List<string> options, List<int> correctAnswerIndexes)
        : base(header, body, marks)
    {
        Type = QuestionType.ChooseAll;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        CorrectAnswerIndexes = correctAnswerIndexes ?? throw new ArgumentNullException(nameof(correctAnswerIndexes));
    }

    public override void Display()
    {
        Console.WriteLine($"{Header}");
        Console.WriteLine($"{Body}");
        Console.WriteLine("(Select all that apply)");
        for (int i = 0; i < Options.Count; i++)
        {
            Console.WriteLine($"{(char)('a' + i)}) {Options[i]}");
        }
        Console.WriteLine($"Marks: {Marks}");
    }

    public override object Clone()
    {
        return new ChooseAllQuestion(Header, Body, Marks, new List<string>(Options), new List<int>(CorrectAnswerIndexes));
    }
}

// Answer class
public class Answer
{
    public int QuestionIndex { get; set; }
    public object Response { get; set; }
    public DateTime AnsweredAt { get; set; }

    public Answer(int questionIndex, object response)
    {
        QuestionIndex = questionIndex;
        Response = response;
        AnsweredAt = DateTime.Now;
    }

    public override string ToString()
    {
        return $"Q{QuestionIndex}: {Response} (at {AnsweredAt:HH:mm:ss})";
    }
}

// Answer List class
public class AnswerList : List<Answer>
{
    public void AddAnswer(int questionIndex, object response)
    {
        Add(new Answer(questionIndex, response));
    }

    public Answer GetAnswerForQuestion(int questionIndex)
    {
        return this.FirstOrDefault(a => a.QuestionIndex == questionIndex);
    }
}

// Question List class that logs to file
public class QuestionList : List<Question>
{
    private string _logFileName;
    private static int _fileCounter = 1;

    public QuestionList()
    {
        _logFileName = $"QuestionLog_{_fileCounter++}.txt";
    }

    public new void Add(Question question)
    {
        // Keep default behavior
        base.Add(question);

        // Log to file
        LogQuestionToFile(question);
    }

    private void LogQuestionToFile(Question question)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(_logFileName, true))
            {
                writer.WriteLine($"[{DateTime.Now}] {question}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging question: {ex.Message}");
        }
    }

    public void LoadQuestionsFromFile(string fileName)
    {
        try
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"Loaded: {line}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
        }
    }
}

// Subject class
public class Subject
{
    public string Name { get; set; }
    public string Code { get; set; }
    public int CreditHours { get; set; }

    public Subject(string name, string code, int creditHours)
    {
        Name = name;
        Code = code;
        CreditHours = creditHours;
    }

    public override string ToString()
    {
        return $"{Code}: {Name} ({CreditHours} credit hours)";
    }
}

// Student class for notification system
public class Student
{
    public string Name { get; set; }
    public string Id { get; set; }
    public Subject Subject { get; set; }

    public Student(string name, string id, Subject subject)
    {
        Name = name;
        Id = id;
        Subject = subject;
    }

    public void OnExamStarting(object sender, ExamEventArgs e)
    {
        Console.WriteLine($"📧 Notification to {Name}: {e.Message}");
    }

    public override string ToString()
    {
        return $"{Name} ({Id}) - {Subject.Name}";
    }
}

// Event arguments for exam notifications
public class ExamEventArgs : EventArgs
{
    public string Message { get; set; }
    public Exam Exam { get; set; }

    public ExamEventArgs(string message, Exam exam)
    {
        Message = message;
        Exam = exam;
    }
}

// Base Exam class
public abstract class Exam : ICloneable, IComparable<Exam>
{
    public TimeSpan Duration { get; set; }
    public int NumberOfQuestions => Questions.Count;
    public QuestionList Questions { get; set; }
    public Dictionary<Question, Answer> QuestionAnswerDictionary { get; set; }
    public Subject Subject { get; set; }
    public ExamMode Mode { get; set; }
    public string ExamTitle { get; set; }

    // Event and delegate for notifications
    public event EventHandler<ExamEventArgs> ExamStarting;

    protected Exam(string title, Subject subject, TimeSpan duration) : this(title, subject, duration, new QuestionList())
    {
    }

    protected Exam(string title, Subject subject, TimeSpan duration, QuestionList questions)
    {
        ExamTitle = title ?? throw new ArgumentNullException(nameof(title));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Duration = duration;
        Questions = questions ?? new QuestionList();
        QuestionAnswerDictionary = new Dictionary<Question, Answer>();
        Mode = ExamMode.Starting;
    }

    public abstract void ShowExam();

    public virtual void StartExam()
    {
        Mode = ExamMode.Starting;
        OnExamStarting($"Exam '{ExamTitle}' for {Subject.Name} is starting!");
        Mode = ExamMode.Queued;
    }

    protected virtual void OnExamStarting(string message)
    {
        ExamStarting?.Invoke(this, new ExamEventArgs(message, this));
    }

    public virtual void FinishExam()
    {
        Mode = ExamMode.Finished;
        Console.WriteLine($"\n🏁 Exam '{ExamTitle}' has been completed!");
    }

    public virtual void AddQuestion(Question question)
    {
        Questions.Add(question);
    }

    public abstract object Clone();

    public virtual int CompareTo(Exam other)
    {
        if (other == null) return 1;
        return Duration.CompareTo(other.Duration);
    }

    public override bool Equals(object obj)
    {
        if (obj is Exam other)
            return ExamTitle == other.ExamTitle && Subject.Code == other.Subject.Code;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExamTitle, Subject.Code);
    }

    public override string ToString()
    {
        return $"{ExamTitle} - {Subject.Name} ({Duration.TotalMinutes} minutes, {NumberOfQuestions} questions)";
    }
}

// Practice Exam class
public class PracticeExam : Exam
{
    public PracticeExam(string title, Subject subject, TimeSpan duration)
        : base(title, subject, duration)
    {
    }

    public PracticeExam(string title, Subject subject, TimeSpan duration, QuestionList questions)
        : base(title, subject, duration, questions)
    {
    }

    public override void ShowExam()
    {
        Console.WriteLine($"\n PRACTICE EXAM: {ExamTitle}");
        Console.WriteLine($"Subject: {Subject}");
        Console.WriteLine($"Duration: {Duration.TotalMinutes} minutes");
        Console.WriteLine($"Mode: {Mode}");
        Console.WriteLine(new string('=', 50));

        for (int i = 0; i < Questions.Count; i++)
        {
            Console.WriteLine($"\nQuestion {i + 1}:");
            Questions[i].Display();

            // In practice exam, show answers after completion
            if (Mode == ExamMode.Finished)
            {
                ShowCorrectAnswer(Questions[i]);
            }
            Console.WriteLine(new string('-', 30));
        }
    }

    private void ShowCorrectAnswer(Question question)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        switch (question)
        {
            case TrueFalseQuestion tfq:
                Console.WriteLine($"✓ Correct Answer: {(tfq.CorrectAnswer ? "True" : "False")}");
                break;
            case ChooseOneQuestion coq:
                Console.WriteLine($"✓ Correct Answer: {(char)('a' + coq.CorrectAnswerIndex)}) {coq.Options[coq.CorrectAnswerIndex]}");
                break;
            case ChooseAllQuestion caq:
                Console.Write("✓ Correct Answers: ");
                foreach (int index in caq.CorrectAnswerIndexes)
                {
                    Console.Write($"{(char)('a' + index)}) {caq.Options[index]} ");
                }
                Console.WriteLine();
                break;
        }
        Console.ResetColor();
    }

    public override object Clone()
    {
        var clonedQuestions = new QuestionList();
        foreach (var question in Questions)
        {
            clonedQuestions.Add((Question)question.Clone());
        }
        return new PracticeExam(ExamTitle, Subject, Duration, clonedQuestions);
    }
}

// Final Exam class
public class FinalExam : Exam
{
    public FinalExam(string title, Subject subject, TimeSpan duration)
        : base(title, subject, duration)
    {
    }

    public FinalExam(string title, Subject subject, TimeSpan duration, QuestionList questions)
        : base(title, subject, duration, questions)
    {
    }

    public override void ShowExam()
    {
        Console.WriteLine($"\n FINAL EXAM: {ExamTitle}");
        Console.WriteLine($"Subject: {Subject}");
        Console.WriteLine($"Duration: {Duration.TotalMinutes} minutes");
        Console.WriteLine($"Mode: {Mode}");
        Console.WriteLine("No answers will be shown during or after this exam!");
        Console.WriteLine(new string('=', 50));

        for (int i = 0; i < Questions.Count; i++)
        {
            Console.WriteLine($"\nQuestion {i + 1}:");
            Questions[i].Display();
            Console.WriteLine(new string('-', 30));
        }
    }

    public override object Clone()
    {
        var clonedQuestions = new QuestionList();
        foreach (var question in Questions)
        {
            clonedQuestions.Add((Question)question.Clone());
        }
        return new FinalExam(ExamTitle, Subject, Duration, clonedQuestions);
    }
}

// Main Program
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(" Welcome to the Examination System!");
        Console.WriteLine(new string('=', 50));

        // Create subjects
        var mathSubject = new Subject("Advanced Mathematics", "MATH301", 3);
        var csSubject = new Subject("Computer Science Fundamentals", "CS101", 4);

        // Create students for notification
        var student1 = new Student("Alice Johnson", "ST001", mathSubject);
        var student2 = new Student("Bob Smith", "ST002", mathSubject);
        var student3 = new Student("Carol Davis", "ST003", csSubject);

        // Create sample questions
        var questions1 = new QuestionList();
        questions1.Add(new TrueFalseQuestion(
            "Basic Algebra",
            "Is the equation 2x + 3 = 7 solved by x = 2?",
            5,
            true));

        questions1.Add(new ChooseOneQuestion(
            "Calculus",
            "What is the derivative of x²?",
            10,
            new List<string> { "2x", "x", "2", "x²" },
            0));

        var questions2 = new QuestionList();
        questions2.Add(new ChooseAllQuestion(
            "Programming Concepts",
            "Which of the following are object-oriented programming principles?",
            15,
            new List<string> { "Encapsulation", "Recursion", "Inheritance", "Polymorphism", "Sorting" },
            new List<int> { 0, 2, 3 }));

        questions2.Add(new TrueFalseQuestion(
            "Data Structures",
            "A stack follows LIFO (Last In, First Out) principle.",
            5,
            true));

        // Create exams
        var practiceExam = new PracticeExam("Midterm Practice", mathSubject, TimeSpan.FromMinutes(60), questions1);
        var finalExam = new FinalExam("Final Examination", csSubject, TimeSpan.FromMinutes(120), questions2);

        // Subscribe students to exam notifications
        practiceExam.ExamStarting += student1.OnExamStarting;
        practiceExam.ExamStarting += student2.OnExamStarting;
        finalExam.ExamStarting += student3.OnExamStarting;

        // Display exam information
        Console.WriteLine(" Available Exams:");
        Console.WriteLine($"1. {practiceExam}");
        Console.WriteLine($"2. {finalExam}");

        // User selection
        while (true)
        {
            Console.WriteLine("\n Select Exam Type:");
            Console.WriteLine("1. Practice Exam");
            Console.WriteLine("2. Final Exam");
            Console.WriteLine("3. Exit");
            Console.Write("Enter your choice (1-3): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n Starting Practice Exam...");
                    practiceExam.StartExam();
                    practiceExam.ShowExam();
                    practiceExam.FinishExam();
                    break;

                case "2":
                    Console.WriteLine("\n Starting Final Exam...");
                    finalExam.StartExam();
                    finalExam.ShowExam();
                    finalExam.FinishExam();
                    break;

                case "3":
                    Console.WriteLine("Thank you for using the Examination System!");
                    return;

                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }
}
