import Markdown, { type Components } from "react-markdown";
import remarkBreaks from "remark-breaks";
import remarkGfm from "remark-gfm";

// gfm: таблицы, списки задач, зачёркивание; breaks: одиночный \n превращается в перенос строки.
const remarkPlugins = [remarkGfm, remarkBreaks];

const components: Components = {
  // ссылки из ответа модели открываем в новой вкладке, не уводя со страницы
  a: ({ href, children }) => (
    <a href={href} target="_blank" rel="noopener noreferrer">
      {children}
    </a>
  ),
  // широкая таблица прокручивается внутри пузыря, а не раздвигает страницу
  table: ({ children }) => (
    <div className="ch-table">
      <table>{children}</table>
    </div>
  ),
};

interface Props {
  text: string;
  /** true для ответов модели (markdown), false для того, что набрал пользователь (как есть). */
  markdown: boolean;
}

export default function MessageContent({ text, markdown }: Props) {
  if (!markdown) return <div className="ch-text">{text}</div>;

  // Сырой HTML из ответа не выполняется: react-markdown выводит его как текст.
  return (
    <div className="ch-md">
      <Markdown remarkPlugins={remarkPlugins} components={components}>
        {text}
      </Markdown>
    </div>
  );
}
