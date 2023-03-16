using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using FirebirdTestDesign.Models;

namespace FirebirdTestDesign.Context
{
    public partial class FirebirdEntities : DbContext
    {
        public FirebirdEntities()
        {
        }

        public FirebirdEntities(DbContextOptions<FirebirdEntities> options)
            : base(options)
        {
        }

        public virtual DbSet<COUNTRY> COUNTRies { get; set; }
        public virtual DbSet<CUSTOMER> CUSTOMERs { get; set; }
        public virtual DbSet<DEPARTMENT> DEPARTMENTs { get; set; }
        public virtual DbSet<EMPLOYEE> EMPLOYEEs { get; set; }
        public virtual DbSet<JOB> JOBs { get; set; }
        public virtual DbSet<PROJECT> PROJECTs { get; set; }
        public virtual DbSet<PROJ_DEPT_BUDGET> PROJ_DEPT_BUDGETs { get; set; }
        public virtual DbSet<SALARY_HISTORY> SALARY_HISTORies { get; set; }
        public virtual DbSet<SALES> SALEs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseFirebird("User=SYSDBA;Password=masterkey;Database=employee;DataSource=localhost;Port=3050;Dialect=3;Charset=NONE;Role=;Connection lifetime=15;Pooling=true;MinPoolSize=0;MaxPoolSize=50;PacketSize=8192;ServerType=0;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<COUNTRY>(entity =>
            {
                entity.HasKey(e => e.COUNTRY1)
                    .HasName("RDB$PRIMARY1");

                entity.ToTable("COUNTRY");

                entity.HasIndex(e => e.COUNTRY1, "RDB$PRIMARY1")
                    .IsUnique();

                entity.Property(e => e.COUNTRY1)
                    .HasMaxLength(15)
                    .HasColumnName("COUNTRY");

                entity.Property(e => e.CURRENCY)
                    .IsRequired()
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<CUSTOMER>(entity =>
            {
                entity.HasKey(e => e.CUST_NO)
                    .HasName("RDB$PRIMARY22");

                entity.ToTable("CUSTOMER");

                entity.HasIndex(e => e.CUSTOMER1, "CUSTNAMEX");

                entity.HasIndex(e => new { e.CITY, e.COUNTRY }, "CUSTREGION");

                entity.HasIndex(e => e.COUNTRY, "RDB$FOREIGN23");

                entity.HasIndex(e => e.CUST_NO, "RDB$PRIMARY22")
                    .IsUnique();

                entity.Property(e => e.ADDRESS_LINE1).HasMaxLength(30);

                entity.Property(e => e.ADDRESS_LINE2).HasMaxLength(30);

                entity.Property(e => e.CITY).HasMaxLength(25);

                entity.Property(e => e.CONTACT_FIRST).HasMaxLength(15);

                entity.Property(e => e.CONTACT_LAST).HasMaxLength(20);

                entity.Property(e => e.COUNTRY).HasMaxLength(15);

                entity.Property(e => e.CUSTOMER1)
                    .IsRequired()
                    .HasMaxLength(25)
                    .HasColumnName("CUSTOMER");

                entity.Property(e => e.ON_HOLD).HasColumnType("CHAR(1) ");

                entity.Property(e => e.PHONE_NO).HasMaxLength(20);

                entity.Property(e => e.POSTAL_CODE).HasMaxLength(12);

                entity.Property(e => e.STATE_PROVINCE).HasMaxLength(15);

                entity.HasOne(d => d.COUNTRY1)
                    .WithMany(p => p.CUSTOMER)
                    .HasForeignKey(d => d.COUNTRY)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_61");
            });

            modelBuilder.Entity<DEPARTMENT>(entity =>
            {
                entity.HasKey(e => e.DEPT_NO)
                    .HasName("RDB$PRIMARY5");

                entity.ToTable("DEPARTMENT");

                entity.HasIndex(e => e.BUDGET, "BUDGETX");

                entity.HasIndex(e => e.DEPARTMENT1, "RDB$4")
                    .IsUnique();

                entity.HasIndex(e => e.MNGR_NO, "RDB$FOREIGN10");

                entity.HasIndex(e => e.HEAD_DEPT, "RDB$FOREIGN6");

                entity.HasIndex(e => e.DEPT_NO, "RDB$PRIMARY5")
                    .IsUnique();

                entity.Property(e => e.DEPT_NO).HasColumnType("CHAR(3) ");

                entity.Property(e => e.BUDGET).HasColumnType("DECIMAL");

                entity.Property(e => e.DEPARTMENT1)
                    .IsRequired()
                    .HasMaxLength(25)
                    .HasColumnName("DEPARTMENT");

                entity.Property(e => e.HEAD_DEPT).HasColumnType("CHAR(3) ");

                entity.Property(e => e.LOCATION).HasMaxLength(15);

                entity.Property(e => e.PHONE_NO).HasMaxLength(20);

                entity.HasOne(d => d.DEPARTMENT2)
                    .WithMany(p => p.DEPARTMENT11)
                    .HasForeignKey(d => d.HEAD_DEPT)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_17");

                entity.HasOne(d => d.EMPLOYEE1)
                    .WithMany(p => p.DEPARTMENT1)
                    .HasForeignKey(d => d.MNGR_NO)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_31");
            });

            modelBuilder.Entity<EMPLOYEE>(entity =>
            {
                entity.HasKey(e => e.EMP_NO)
                    .HasName("RDB$PRIMARY7");

                entity.ToTable("EMPLOYEE");

                entity.HasIndex(e => new { e.FIRST_NAME, e.LAST_NAME }, "NAMEX");

                entity.HasIndex(e => e.DEPT_NO, "RDB$FOREIGN8");

                entity.HasIndex(e => new { e.JOB_GRADE, e.JOB_CODE, e.JOB_COUNTRY }, "RDB$FOREIGN9");

                entity.HasIndex(e => e.EMP_NO, "RDB$PRIMARY7")
                    .IsUnique();

                entity.Property(e => e.DEPT_NO)
                    .IsRequired()
                    .HasColumnType("CHAR(3) ");

                entity.Property(e => e.FIRST_NAME)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.FULL_NAME).HasMaxLength(37);

                entity.Property(e => e.JOB_CODE)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(e => e.JOB_COUNTRY)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.LAST_NAME)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PHONE_EXT).HasMaxLength(4);

                entity.Property(e => e.SALARY).HasColumnType("DECIMAL");

                entity.HasOne(d => d.DEPARTMENT)
                    .WithMany(p => p.EMPLOYEE)
                    .HasForeignKey(d => d.DEPT_NO)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_28");

                entity.HasOne(d => d.JOB)
                    .WithMany(p => p.EMPLOYEE)
                    .HasForeignKey(d => new { d.JOB_CODE, d.JOB_GRADE, d.JOB_COUNTRY })
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_29");

                entity.HasMany(d => d.Projs)
                    .WithMany(p => p.EmpNos)
                    .UsingEntity<Dictionary<string, object>>(
                        "EmployeeProject",
                        l => l.HasOne<PROJECT>().WithMany().HasForeignKey("ProjId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("INTEG_41"),
                        r => r.HasOne<EMPLOYEE>().WithMany().HasForeignKey("EmpNo").OnDelete(DeleteBehavior.Restrict).HasConstraintName("INTEG_40"),
                        j =>
                        {
                            j.HasKey("EmpNo", "ProjId").HasName("RDB$PRIMARY14");

                            j.ToTable("EMPLOYEE_PROJECT");

                            j.HasIndex(new[] { "EmpNo" }, "RDB$FOREIGN15");

                            j.HasIndex(new[] { "ProjId" }, "RDB$FOREIGN16");

                            j.HasIndex(new[] { "ProjId", "EmpNo" }, "RDB$PRIMARY14").IsUnique();

                            j.IndexerProperty<short>("EmpNo").HasColumnName("EMP_NO");

                            j.IndexerProperty<string>("ProjId").HasColumnType("CHAR(5) ").HasColumnName("PROJ_ID");
                        });
            });

            modelBuilder.Entity<JOB>(entity =>
            {
                entity.HasKey(e => new { e.JOB_CODE, e.JOB_GRADE, e.JOB_COUNTRY })
                    .HasName("RDB$PRIMARY2");

                entity.ToTable("JOB");

                entity.HasIndex(e => new { e.JOB_COUNTRY, e.MAX_SALARY }, "MAXSALX");

                entity.HasIndex(e => new { e.JOB_COUNTRY, e.MIN_SALARY }, "MINSALX");

                entity.HasIndex(e => e.JOB_COUNTRY, "RDB$FOREIGN3");

                entity.HasIndex(e => new { e.JOB_GRADE, e.JOB_CODE, e.JOB_COUNTRY }, "RDB$PRIMARY2")
                    .IsUnique();

                entity.Property(e => e.JOB_CODE).HasMaxLength(5);

                entity.Property(e => e.JOB_COUNTRY).HasMaxLength(15);

                entity.Property(e => e.JOB_TITLE)
                    .IsRequired()
                    .HasMaxLength(25);

                entity.Property(e => e.LANGUAGE_REQ).HasMaxLength(15);

                entity.Property(e => e.MAX_SALARY).HasColumnType("DECIMAL");

                entity.Property(e => e.MIN_SALARY).HasColumnType("DECIMAL");

                entity.HasOne(d => d.COUNTRY)
                    .WithMany(p => p.JOB)
                    .HasForeignKey(d => d.JOB_COUNTRY)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_11");
            });

            modelBuilder.Entity<PROJECT>(entity =>
            {
                entity.HasKey(e => e.PROJ_ID)
                    .HasName("RDB$PRIMARY12");

                entity.ToTable("PROJECT");

                entity.HasIndex(e => new { e.PROJ_NAME, e.PRODUCT }, "PRODTYPEX")
                    .IsUnique();

                entity.HasIndex(e => e.PROJ_NAME, "RDB$11")
                    .IsUnique();

                entity.HasIndex(e => e.TEAM_LEADER, "RDB$FOREIGN13");

                entity.HasIndex(e => e.PROJ_ID, "RDB$PRIMARY12")
                    .IsUnique();

                entity.Property(e => e.PROJ_ID).HasColumnType("CHAR(5) ");

                entity.Property(e => e.PRODUCT).HasMaxLength(12);

                entity.Property(e => e.PROJ_NAME)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasOne(d => d.EMPLOYEE)
                    .WithMany(p => p.PROJECT)
                    .HasForeignKey(d => d.TEAM_LEADER)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_36");
            });

            modelBuilder.Entity<PROJ_DEPT_BUDGET>(entity =>
            {
                entity.HasKey(e => new { e.FISCAL_YEAR, e.PROJ_ID, e.DEPT_NO })
                    .HasName("RDB$PRIMARY17");

                entity.ToTable("PROJ_DEPT_BUDGET");

                entity.HasIndex(e => e.DEPT_NO, "RDB$FOREIGN18");

                entity.HasIndex(e => e.PROJ_ID, "RDB$FOREIGN19");

                entity.HasIndex(e => new { e.PROJ_ID, e.DEPT_NO, e.FISCAL_YEAR }, "RDB$PRIMARY17")
                    .IsUnique();

                entity.Property(e => e.PROJ_ID).HasColumnType("CHAR(5) ");

                entity.Property(e => e.DEPT_NO).HasColumnType("CHAR(3) ");

                entity.Property(e => e.PROJECTED_BUDGET).HasColumnType("DECIMAL");

                entity.HasOne(d => d.DEPARTMENT)
                    .WithMany(p => p.PROJ_DEPT_BUDGET)
                    .HasForeignKey(d => d.DEPT_NO)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_47");

                entity.HasOne(d => d.PROJECT)
                    .WithMany(p => p.PROJ_DEPT_BUDGET)
                    .HasForeignKey(d => d.PROJ_ID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_48");
            });

            modelBuilder.Entity<SALARY_HISTORY>(entity =>
            {
                entity.HasKey(e => new { e.EMP_NO, e.CHANGE_DATE, e.UPDATER_ID })
                    .HasName("RDB$PRIMARY20");

                entity.ToTable("SALARY_HISTORY");

                entity.HasIndex(e => e.CHANGE_DATE, "CHANGEX");

                entity.HasIndex(e => e.EMP_NO, "RDB$FOREIGN21");

                entity.HasIndex(e => new { e.CHANGE_DATE, e.UPDATER_ID, e.EMP_NO }, "RDB$PRIMARY20")
                    .IsUnique();

                entity.HasIndex(e => e.UPDATER_ID, "UPDATERX");

                entity.Property(e => e.UPDATER_ID).HasMaxLength(20);

                entity.Property(e => e.OLD_SALARY).HasColumnType("DECIMAL");

                entity.HasOne(d => d.EMPLOYEE)
                    .WithMany(p => p.SALARY_HISTORY)
                    .HasForeignKey(d => d.EMP_NO)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_56");
            });

            modelBuilder.Entity<SALES>(entity =>
            {
                entity.HasKey(e => e.PO_NUMBER)
                    .HasName("RDB$PRIMARY24");

                entity.ToTable("SALES");

                entity.HasIndex(e => e.DATE_NEEDED, "NEEDX");

                entity.HasIndex(e => new { e.ITEM_TYPE, e.QTY_ORDERED }, "QTYX");

                entity.HasIndex(e => e.CUST_NO, "RDB$FOREIGN25");

                entity.HasIndex(e => e.SALES_REP, "RDB$FOREIGN26");

                entity.HasIndex(e => e.PO_NUMBER, "RDB$PRIMARY24")
                    .IsUnique();

                entity.HasIndex(e => new { e.ORDER_STATUS, e.PAID }, "SALESTATX");

                entity.Property(e => e.PO_NUMBER).HasColumnType("CHAR(8) ");

                entity.Property(e => e.AGED).HasColumnType("DECIMAL");

                entity.Property(e => e.ITEM_TYPE).HasMaxLength(12);

                entity.Property(e => e.ORDER_STATUS)
                    .IsRequired()
                    .HasMaxLength(7);

                entity.Property(e => e.PAID).HasColumnType("CHAR(1) ");

                entity.Property(e => e.TOTAL_VALUE).HasColumnType("DECIMAL");

                entity.HasOne(d => d.CUSTOMER)
                    .WithMany(p => p.SALES)
                    .HasForeignKey(d => d.CUST_NO)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_77");

                entity.HasOne(d => d.EMPLOYEE)
                    .WithMany(p => p.SALES)
                    .HasForeignKey(d => d.SALES_REP)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("INTEG_78");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
