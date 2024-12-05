using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bibliotec_mvc.Contexts;
using Bibliotec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bibliotec_mvc.Controllers
{
    [Route("[controller]")]
    public class LivroController : Controller
    {
        private readonly ILogger<LivroController> _logger;

        public LivroController(ILogger<LivroController> logger)
        {
            _logger = logger;
        }

        Context context = new Context();

        public IActionResult Index()
        {
            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            //Criar uma lista de livros
            List<Livro> listaLivros = context.Livro.ToList();

            //Verificar se o livro tem reserva ou não
            //ToDictionay(chave, valor)
            var livrosReservados = context.LivroReserva.ToDictionary
            (livro => livro.LivroID, livror => livror.DtReserva);
            ViewBag.CategoriasDoSistema = context.Categoria.ToList();
            ViewBag.Livros = listaLivros;
            ViewBag.LivrosComReserva = livrosReservados;

            return View();
        }
        [Route("Cadastro")]
        //Método que retorna a tela de cadastro:
        public IActionResult Cadastro()
        {
            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;
            ViewBag.Categorias = context.Categoria.ToList();
            return View();
        }
        [Route("Cadastrar")]
        public IActionResult Cadastrar(IFormCollection form)
        {
            Livro novoLivro = new Livro();


            novoLivro.Nome = form["Nome"].ToString();
            novoLivro.Descricao = form["Descricao"].ToString();
            novoLivro.Editora = form["Editora"].ToString();
            novoLivro.Escritor = form["Escritor"].ToString();
            novoLivro.Idioma = form["Idioma"].ToString();

            if (form.Files.Count > 0)
            {

                var arquivo = form.Files[0];
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Livros");

                if (!Directory.Exists(pasta))
                {

                    Directory.CreateDirectory(pasta);
                }
                var caminho = Path.Combine(pasta, arquivo.FileName);

                using (var stream = new FileStream(caminho, FileMode.Create))
                {
                    arquivo.CopyTo(stream);

                }
                novoLivro.Imagem = arquivo.FileName;
            }
            else
            {
                novoLivro.Imagem = "padrao.png";
            }



            context.Livro.Add(novoLivro);
            context.SaveChanges();

            List<LivroCategoria> listalivroCategorias = new List<LivroCategoria>()!;

            string[] categoriasSelecionadas = form["Categoria"].ToString().Split(',');


            foreach (string categoria in categoriasSelecionadas)
            {

                LivroCategoria livroCategoria = new LivroCategoria();


                livroCategoria.CategoriaID = int.Parse(categoria);
                livroCategoria.LivroID = novoLivro.LivroID;
                listalivroCategorias.Add(livroCategoria);

            }
            context.LivroCategoria.AddRange(listalivroCategorias);
            context.SaveChanges();

            return LocalRedirect("/Cadastro");
            return View();
        }


        [Route("Editar/{id}")]
        public IActionResult Editar(int id)
        {
            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;
            ViewBag.CategoriasDoSistema = context.Categoria.ToList();


            Livro livroAtualizado = context.Livro.FirstOrDefault
            (livro => livro.LivroID == id)!;

            var categoriasDoLivroAtlivroAtualizado = context.LivroCategoria.Where
            (indentificadorLivro => indentificadorLivro.LivroID == id).Select(livro => livro.Categoria).ToList();
            ViewBag.Livro = livroAtualizado;
            ViewBag.Categoria = categoriasDoLivroAtlivroAtualizado;



            return View();

        }
        [Route("Atualizar/{id}")]
        public IActionResult Atualizar(IFormCollection form, int id, IFormFile imagem)
        {

            Livro livroAtualizado = context.Livro.FirstOrDefault(livro => livro.LivroID == id)!;

            livroAtualizado.Nome = form["Nome"];
            livroAtualizado.Escritor = form["Escritor"];
            livroAtualizado.Editora = form["Editora"];
            livroAtualizado.Descricao = form["descricao"];
            livroAtualizado.Idioma = form["Idioma"];

            if (imagem != null && imagem.Length > 0)
            {
                var caminhoImagem = Path.Combine("wwroot/imagem/Livros", imagem.FileName);




                if (string.IsNullOrEmpty(livroAtualizado.Imagem))
                {
                    var caminhoImagemAntiga = Path.Combine("wwroot/imagem/Livros", livroAtualizado.Imagem);



                    if (System.IO.File.Exists(caminhoImagemAntiga))
                    {
                        System.IO.File.Delete(caminhoImagemAntiga);
                    }
                }
                using (var stream = new FileStream(caminhoImagem, FileMode.Create))
                {
                    imagem.CopyTo(stream);
                }
                livroAtualizado.Imagem = imagem.FileName;
            }
            //Categorias 
            // 1) precisamos pegar as categorias selecionadas do Usuario
            var categoriasSelecionadas = form["Categoria"].ToList();
            // 2) Pegaremos as categorias Atuais do Livro
            var categoriasAtuais = context.LivroCategoria.Where(Livro => Livro.LivroID == id).ToList();
            // 3) Removeremos as categorias antigas
            foreach (var categoria in categoriasAtuais)
            {
                if (!categoriasSelecionadas.Contains(categoria.CategoriaID.ToString()))
                {
                    context.LivroCategoria.Remove(categoria);
                }
            }
            // 4)Acionaremos as novas categorias

            foreach (var categoria in categoriasSelecionadas)
            {
                if (!categoriasAtuais.Any(c => c.CategoriaID.ToString() == categoria))
                {
                    context.LivroCategoria.Add(new LivroCategoria
                    {
                        LivroID = id,
                        CategoriaID = int.Parse(categoria)
                    });
                }
            }

            return View();
        }
        [Route("Excluir/{id}")]
        public IActionResult Excluir(int id)
        {
            Livro livroEncontrado = context.Livro.First(livro => livro.LivroID == id);
            var categoriaDoLivro = context.LivroCategoria.Where(livro => livro.LivroID == id).ToList();
            foreach (var categoria in categoriaDoLivro)
            {
                context.LivroCategoria.Remove(categoria);
                context.SaveChanges();
            }
            context.Livro.Remove(livroEncontrado);
            return LocalRedirect("/livro");

     }

    }
}
