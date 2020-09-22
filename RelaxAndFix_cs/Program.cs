using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Gurobi;
using System.Diagnostics;

namespace RelaxAndFix_cs
{
    //classe com todos os parâmetros
    public class Parametros
    {
        public int crew { get; set; }
        public int task { get; set; }
        public int date { get; set; }
        public int competencies { get; set; }
        public int technical_place { get; set; }
        public double hours_per_shift { get; set; }
        public int level_total { get; set; }
        public double[] c { get; set; }
        public int[] d1 { get; set; }
        public int[] d2 { get; set; }
        public int[,] e { get; set; }
        public int[,] bo { get; set; }
        public int[,] bm { get; set; }
        public int[,] bm2 { get; set; }
        public int[,] bm3 { get; set; }
        public int[,] tp { get; set; }
        public double[,] tr { get; set; }
        public double[] tm { get; set; }
        public int[] g { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //Deserializar o arquivo Json para o c#
            //JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var json = File.ReadAllText("C:\\Users\\ruany\\Documents\\dissertacao\\gerador-instancias\\instanciaT11E8D10F6.json");
            Parametros instancia = JsonConvert.DeserializeObject<Parametros>(json);

            // matrizes que irão guardar as soluções do Relax and Fix
            double[,,] matriz_RaF_x = new double[instancia.crew, instancia.task, instancia.date];
            double[,] matriz_RaF_x3 = new double[instancia.task, instancia.date];
            double[,] matriz_RaF_x2 = new double[instancia.task, instancia.date];
            double[] matriz_RaF_x4 = new double[instancia.task];
            double[,,] matriz_RaF_x5 = new double[instancia.crew, instancia.task, instancia.date];
            double[,] matriz_RaF_x6 = new double[instancia.task, instancia.date];
            double[,] matriz_RaF_y = new double[instancia.crew, instancia.date];
            double[,,] matriz_RaF_z = new double[instancia.crew, instancia.task, instancia.date];
            double[,] matriz_RaF_z1 = new double[instancia.crew, instancia.task];
            double[,,] matriz_RaF_w = new double[instancia.crew, instancia.technical_place, instancia.date];
            double[,,,] matriz_RaF_v = new double[instancia.crew, instancia.technical_place, instancia.technical_place, instancia.date];
            double[,,] matriz_RaF_w1 = new double[instancia.crew, instancia.technical_place, instancia.date];
            double[,,] matriz_RaF_w2 = new double[instancia.crew, instancia.technical_place, instancia.date];

            //tempo por iteração
            double tempo_iteracao, tempo_total;
            //status de cada iteração
            int status_RF;
            //lista
            List<double> lista_RF = new List<double>();
            double soma_RF = 0;

            //função Relax and Fix
            void modelo_RelaxAndFixData(int iteracao)
            {
                //Modelo
                GRBEnv ambiente = new GRBEnv();
                GRBModel modelo = new GRBModel(ambiente);

                //número grande
                int M = 1000;

                //Variáveis

                //fração da tarefa i que o membro de equipe n completa na data j
                GRBVar[,,] x = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            x[n, i, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "x_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //fração da tarefa i que é completada na data j
                GRBVar[,] x3 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x3[i, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "x3_" + i + "_" + j);
                    }
                }
                //1 se alguma tarefa i é completada na data j
                GRBVar[,] x2 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x2[i, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "x2_" + i + "_" + j);
                    }
                }
                //1 se a tarefa i é concluída dentro do horizonte de planejamento
                GRBVar[] x4 = new GRBVar[instancia.task];
                for (int i = 0; i < instancia.task; i++)
                {
                    x4[i] = modelo.AddVar(0, 1, instancia.c[i] * 2, GRB.CONTINUOUS, "x4_" + i);
                }
                //variável fantasma
                GRBVar[] vf = new GRBVar[instancia.task];
                for (int i = 0; i < instancia.task; i++)
                {
                    vf[i] = modelo.AddVar(1, 1, instancia.c[i] * 2, GRB.BINARY, "vf_" + i);
                }
                //1 se o membro de equipe n está trabalhando na tarefa i na data j mas não na data j+1
                GRBVar[,,] x5 = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            x5[n, i, j] = modelo.AddVar(0, 1, 0.1, GRB.CONTINUOUS, "x5_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //1 se parte da tarefa i é completada na data j mas não na data j+1
                GRBVar[,] x6 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x6[i, j] = modelo.AddVar(0, 1, 0.9, GRB.CONTINUOUS, "x6_" + i + "_" + j);
                    }
                }
                //1 se o membro da equipe n vai trabalhar na data j
                GRBVar[,] y = new GRBVar[instancia.crew, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        y[n, j] = modelo.AddVar(0, 1, instancia.hours_per_shift, GRB.CONTINUOUS, "y_" + n + "_" + j);
                    }
                }
                //1 se membro da equipe n trabalha na tarefa i na data j
                GRBVar[,,] z = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            z[n, i, j] = modelo.AddVar(0, 1, 0.5, GRB.CONTINUOUS, "z_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //1 se o membro de equipe n trabalha na tarefa i
                GRBVar[,] z1 = new GRBVar[instancia.crew, instancia.task];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        z1[n, i] = modelo.AddVar(0, 1, 0.1, GRB.CONTINUOUS, "z1_" + n + "_" + i);
                    }
                }
                //1 se o membro de equipe n trabalha no local técnico p na data j
                GRBVar[,,] w = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w[n, p, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "w_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //1 se o membro de equipe n precisa de transporte entre o local técnico o e o local q na instancia.date j
                GRBVar[,,,] v = new GRBVar[instancia.crew, instancia.technical_place, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int q = 0; q < instancia.technical_place; q++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                v[n, p, q, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "v_" + n + "_" + p + "_" + q + "_" + j);
                            }
                        }
                    }
                }
                //se a equipe n precisa de transporte para o local técnico p de outro local técnico na data j
                GRBVar[,,] w1 = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w1[n, p, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "w1_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //se a equipe n precisa de transporte do local técnico p para outro local técnico
                GRBVar[,,] w2 = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w2[n, p, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "w2_" + n + "_" + p + "_" + j);
                        }
                    }
                }

                //Função objetivo
                modelo.ModelSense = GRB.MINIMIZE;


                //Restrições
                GRBLinExpr exp = 0.0;
                GRBLinExpr exp2 = 0.0;
                GRBLinExpr exp3 = 0.0;

                //Restrições com relação a tarefa

                //restrição 2 
                //a tarefa deve ser concluída dentro do horizonte de planejamento
                for (int i = 0; i < instancia.task; i++)
                {
                    exp.Clear();
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                    }
                    modelo.AddConstr(exp == x4[i], "R2_" + i);
                }
                //restrição 3
                //o número total de horas por turno não deve ser excedido
                for (int j = 0; j < instancia.date; j++)
                {
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        exp.Clear();
                        exp2.Clear();
                        exp3.Clear();
                        for (int i = 0; i < instancia.task; i++)
                        {
                            exp.AddTerm(instancia.c[i], x[n, i, j]);
                        }
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            exp2.AddTerm(2 * instancia.tm[p], w[n, p, j]);
                            exp2.AddTerm(-instancia.tm[p], w1[n, p, j]);
                            exp2.AddTerm(-instancia.tm[p], w2[n, p, j]);
                        }
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp3.AddTerm(instancia.tr[p, q], v[n, p, q, j]);
                            }
                        }
                        modelo.AddConstr(exp + exp2 + exp3 <= instancia.hours_per_shift, "R3_" + j + "_" + n);
                    }
                }
                //restrição 4
                //a soma das frações das tarefas locadas  não pode exceder o total para completar a tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                        modelo.AddConstr(x2[i, j] >= exp, "R4_" + i + "_" + j);
                    }
                }
                //restrição 5
                //soma de das frações dos membros e equipe num dado dia deve ser igual a x3
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                        modelo.AddConstr(x3[i, j] == exp, "R5_" + i + "_" + j);
                    }
                }
                //restrição 6
                //a tarefa i deve ser completada dentro do horizonte de planejamento se gi=1
                for (int i = 0; i < instancia.task; i++)
                {
                    modelo.AddConstr(x4[i] >= instancia.g[i], "R6_" + i);
                }
                //restrição 7
                //fração da tarefa que é completada num dado dia não deve exceder X4
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(x4[i] >= x[n, i, j], "R7_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 8
                //um membro de equipe não pode ser locado a uma tarefa em um dia em que ele não trabalha
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(y[n, j] >= z[n, i, j], "R8_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 9
                //se o membro de equipe é locado para uma tarefa então z=1
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] >= x[n, i, j], "R9_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 10
                //a variável z não pode ser 1 se a equipe n não trabalha num dado dia
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] <= M * x[n, i, j], "R10_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 11
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z1[n, i] >= z[n, i, j], "R11_" + n + "_" + i + "_" + j);
                        }
                    }
                }

                //Restrições de gerenciemanto

                //restrição 12
                //preferencalmente uma tarefa deve concluida pela mesma pessoa que começou trabalhando nela
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date - 1; j++)
                        {
                            modelo.AddConstr(x5[n, i, j] >= z[n, i, j] - z[n, i, j + 1], "R12_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 13
                //uma penalidade será dada ao planejamento se a tarefa i é completada em dias não consecutivos
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date - 1; j++)
                    {
                        modelo.AddConstr(x6[i, j] >= x2[i, j] - x2[i, j + 1], "R13_" + i + "_" + j);
                    }
                }
                //restrição 14
                //o número mínimo de membros de equipe que podem trabalhar simultaneamente em uma tarefa 
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, z[n, i, j]);
                        }
                        modelo.AddConstr(exp >= instancia.d1[i] * x2[i, j], "R14_" + i + "_" + j);
                    }
                }
                //restrição 15
                //o número máximo de membros de equipe que podem trablhar simultaneamente em uma tarefa 
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, z[n, i, j]);
                        }
                        modelo.AddConstr(exp <= instancia.d2[i] * x2[i, j], "R15_" + i + "_" + j);
                    }
                }
                //restrição 16
                //número mínimo de membros para trabalhar em um tarefa deve ser respeitado
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(x[n, i, j] <= x3[i, j] / instancia.d1[i], "R16_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 17
                //membros de equipe não podem trabalhar em dias em que eles não estão disponíveis
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] <= instancia.e[n, j], "R17_" + n + "_" + i + "_" + j);
                        }
                    }
                }

                //Restrições com relação a competência

                //restrição 18
                //a combinação do nível de competencias de todos os membros 
                //de equipe deve ser suficiente para cada tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm3[n, k], z[n, i, j]);
                            }
                            modelo.AddConstr(exp >= x2[i, j] * instancia.bo[i, k] * instancia.level_total, "R18_" + i + "_" + j + "_" + k);
                        }
                    }
                }
                //restrição 19
                //pelo menos um membro de equipe deve ter nível 3 de competencia para a tarefa i
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm[n, k], z[n, i, j]);
                            }
                            modelo.AddConstr(exp >= x2[i, j] * instancia.bo[i, k], "R19_" + i + "_" + j + "_" + k);
                        }
                    }
                }
                //restrição 20
                //pelo menos um mebro de equipe tem nível de competencia 3 se vários membros de equipe trabalham na mesma tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            exp2.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm[n, k], x[n, i, j]);
                                exp2.AddTerm(instancia.bm2[n, k], x[n, i, j]);
                            }
                            modelo.AddConstr(exp >= exp2 * (double)(1 / instancia.d1[i]), "R20_" + i + "_" + j + "_" + k);
                        }
                    }
                }

                //Restrições com relação ao transporte

                //restrição 21
                //cada membro de equipe trabalha em um local técnico em que a tarefa está localizada
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int i = 0; i < instancia.task; i++)
                            {
                                exp.AddTerm(instancia.tp[i, p], z[n, i, j]);
                            }
                            modelo.AddConstr(w[n, p, j] <= exp, "R21_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 22
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int i = 0; i < instancia.task; i++)
                            {
                                exp.AddTerm(instancia.tp[i, p], z[n, i, j]);
                            }
                            modelo.AddConstr(w[n, p, j] * M >= exp, "R22_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 23
                //o membro de equipe só é transportado entre os locais técnicos que as tarefas dele estão localizadas
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(exp <= w[n, p, j] * M, "R23_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 24
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int q = 0; q < instancia.technical_place; q++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(exp <= w[n, q, j] * M, "R24_" + n + "_" + q + "_" + j);
                        }
                    }
                }
                //restrição 25
                //se o membro de equipe trabalha em mais do que um local técninco durante o turno
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int q = 0; q < instancia.technical_place; q++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(w1[n, q, j] == exp, "R25_" + n + "_" + q + "_" + j);
                        }
                    }
                }
                //restrição 26
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(w2[n, p, j] == exp, "R26_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 27
                //cada membro de equipe pode apenas ser transportado de e para cada local técnico uma vez por dia
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(w1[n, p, j] <= 1, "R27_" + n + "_" + p + "_" + j);
                        }
                    }
                }

                //restrição 28
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(w2[n, p, j] <= 1, "R28_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 29
                //funcionário será transportado apenas uma vez do e para o depósito 
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            exp.AddTerm(2, w[n, p, j]);
                            exp.AddTerm(-1, w1[n, p, j]);
                            exp.AddTerm(-1, w2[n, p, j]);
                        }
                        modelo.AddConstr(exp == 2 * y[n, j], "R29_" + n + "_" + j);
                    }
                }

                //variáveis a serem fixadas
                if (iteracao != 0)
                {                   
                    for(int n = 0; n < instancia.crew; n++)
                    {
                        //fixar variável y
                        for (int j = 0; j < iteracao - 1; j++)
                        {
                            y[n, j].Set(GRB.DoubleAttr.LB, matriz_RaF_y[n, j]);
                            y[n, j].Set(GRB.DoubleAttr.UB, matriz_RaF_y[n, j]);

                            //fixar variável z
                            for (int i = 0; i < instancia.task; i++)
                            {
                                z[n, i, j].Set(GRB.DoubleAttr.LB, matriz_RaF_z[n, i, j]);
                                z[n, i, j].Set(GRB.DoubleAttr.UB, matriz_RaF_z[n, i, j]);
                            }
                        }
                        
                    }
                }

                //varivais binárias
                //variável z
                for(int n = 0; n < instancia.crew; n++)
                {
                    for(int i = 0; i < instancia.task; i++)
                    {
                        z[n, i, iteracao].Set(GRB.CharAttr.VType, GRB.BINARY);
                    }
                }
                //variável y
                for(int n = 0; n < instancia.crew; n++)
                {
                    y[n, iteracao].Set(GRB.CharAttr.VType, GRB.BINARY);
                }
                //variável x4
                for(int i = 0; i < instancia.task; i++)
                {
                    x4[i].Set(GRB.CharAttr.VType, GRB.BINARY);
                }
                //variável x5
                for(int n = 0; n < instancia.crew; n++)
                {
                    for(int i = 0; i < instancia.task; i++)
                    {
                        for(int j = 0; j < iteracao; j++)
                        {
                            x5[n, i, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                        }
                    }
                }
                //variável x6
                for(int i = 0; i < instancia.task; i++)
                {
                    for(int j = 0; j < iteracao; j++)
                    {
                        x6[i, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                    }
                }
                //variável z1
                for(int n = 0; n < instancia.crew; n++)
                {
                    for(int i = 0; i < instancia.task; i++)
                    {
                        z1[n, i].Set(GRB.CharAttr.VType, GRB.BINARY);
                    }
                }
                //variável w
                for(int n = 0; n < instancia.crew; n++)
                {
                    for(int p = 0; p < instancia.technical_place; p++)
                    {
                        for(int j = 0; j < iteracao; j++)
                        {
                            w[n, p, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                        }
                    }
                }
                //variável v
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for(int q = 0; q < instancia.technical_place; q++)
                        {
                            for (int j = 0; j < iteracao; j++)
                            {
                                v[n, p,q, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                            }
                        }                       
                    }
                }
                //variável w1
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < iteracao; j++)
                        {
                            w1[n, p, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                        }
                    }
                }
                //variável w2
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < iteracao; j++)
                        {
                            w2[n, p, j].Set(GRB.CharAttr.VType, GRB.BINARY);
                        }
                    }
                }

                //otimizar modelo
                modelo.Set(GRB.DoubleParam.TimeLimit, tempo_iteracao);
                modelo.Update();
                modelo.Optimize();
                tempo_total -= modelo.Get(GRB.DoubleAttr.Runtime);

                //atualizar 
                int solverStatus = modelo.Get(GRB.IntAttr.Status);
                if(modelo.SolCount==0 || solverStatus == 3)
                {
                    status_RF = 999999;
                    return;
                }
                else
                {
                    //salvar a salocao 
                    for(int n = 0; n < instancia.crew; n++)
                    {
                        for(int i = 0; i < instancia.task; i++)
                        {
                            //variável z
                            matriz_RaF_z[n, i, iteracao] = z[n, i, iteracao].X;                           
                        }
                        //variável y
                        matriz_RaF_y[n, iteracao] = y[n, iteracao].X;
                    }
                    // se estiver na última iteracao salvar nas matizes o valor das variáveis
                    if (iteracao == instancia.date)
                    {
                        //variável x
                        for(int n = 0; n< instancia.crew; n++)
                        {
                            for(int i = 0; i < instancia.task; i++)
                            {
                                for(int j = 0; j < instancia.date; j++)
                                {
                                    matriz_RaF_x[n, i, j] = x[n, i, j].X;
                                }
                            }
                        }
                        //variável x3
                        for (int i = 0; i < instancia.task; i++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                matriz_RaF_x3[i, j] = x3[i, j].X;
                            }
                        }
                        //vairável x2
                        for (int i = 0; i < instancia.task; i++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                matriz_RaF_x2[i, j] = x2[i, j].X;
                            }
                        }
                        //variável x4
                        for(int i = 0; i < instancia.task; i++)
                        {
                            matriz_RaF_x4[i] = x4[i].X;
                        }
                        //variável x5
                        for(int n = 0; n < instancia.crew; n++)
                        {
                            for(int i = 0; i < instancia.task; i++)
                            {
                                for(int j = 0; j < instancia.date; j++)
                                {
                                    matriz_RaF_x5[n, i, j] = x5[n, i, j].X;
                                }
                            }
                        }
                        //variável x6
                        for (int i = 0; i < instancia.task; i++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                matriz_RaF_x6[i, j] = x6[i, j].X;
                            }
                        }
                        //variável z1
                        for(int n = 0; n < instancia.crew; n++)
                        {
                            for(int i = 0; i < instancia.task; i++)
                            {
                                matriz_RaF_z1[n, i] = z1[n, i].X;
                            }
                        }
                        //variável w
                        for(int n = 0; n < instancia.crew; n++)
                        {
                            for(int p = 0; p < instancia.technical_place; p++)
                            {
                                for(int j = 0; j < instancia.date; j++)
                                {
                                    matriz_RaF_w[n, p, j] = w[n, p, j].X;
                                }
                            }
                        }
                        //variável v
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                for(int q = 0; q < instancia.technical_place; q++)
                                {
                                    for (int j = 0; j < instancia.date; j++)
                                    {
                                        matriz_RaF_v[n, p, q, j] = v[n, p, q, j].X;
                                    }
                                }                               
                            }
                        }
                        //variável w1
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                for (int j = 0; j < instancia.date; j++)
                                {
                                    matriz_RaF_w1[n, p, j] = w1[n, p, j].X;
                                }
                            }
                        }
                        //variável w2
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                for (int j = 0; j < instancia.date; j++)
                                {
                                    matriz_RaF_w2[n, p, j] = w2[n, p, j].X;
                                }
                            }
                        }
                    }
                }
                if (iteracao == instancia.date)
                {
                    double funcaoObjetivo;
                    funcaoObjetivo = modelo.Get(GRB.DoubleAttr.ObjVal);
                    Console.WriteLine("Solução atual:" + funcaoObjetivo.ToString());
                    lista_RF.Add(funcaoObjetivo);
                    soma_RF += funcaoObjetivo;
                }                
            }
            

            //Relaz and Fix
      
            //Stopwatch para contar o tempo
            Stopwatch relogio = new Stopwatch();            

            tempo_total = 3600;
            relogio.Start();
            for(int j = 0; j < instancia.date; j++)
            {
                status_RF = 0;
                tempo_iteracao = tempo_total / (instancia.date - j + 1);
                modelo_RelaxAndFixData(j);   
                if (status_RF != 0)
                {
                    lista_RF.Add(99999999);
                    soma_RF += 99999999;
                }
            }
            relogio.Stop();
            Console.WriteLine("tempo total de:" +(relogio.ElapsedMilliseconds / 1000).ToString()+ "segundos");
            Console.ReadKey();
        }
    }
}
