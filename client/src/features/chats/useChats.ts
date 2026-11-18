import { useCallback, useEffect, useRef, useState } from "react";
import { chatsApi } from "../../api/chats";
import { errorMessage } from "../../api/http";
import type { AiModel, Chat, ChatMessage } from "../../types/chats";

const MAX_TITLE = 60;

/** Название нового чата: начало первого сообщения. */
function titleFrom(text: string): string {
  const oneLine = text.replace(/\s+/g, " ").trim();
  return oneLine.length > MAX_TITLE ? `${oneLine.slice(0, MAX_TITLE - 1)}…` : oneLine;
}

/**
 * Состояние вкладки «Чаты»: модели, чаты выбранной модели, сообщения открытого чата, отправка.
 * Данные грузятся при каждом открытии вкладки (active становится true, как в useStats);
 * при выходе из аккаунта приложение размонтируется вместе с состоянием.
 */
export function useChats(active: boolean) {
  const [models, setModels] = useState<AiModel[]>([]);
  const [modelId, setModelIdState] = useState<string | null>(null);
  const [chats, setChats] = useState<Chat[]>([]);
  const [chatId, setChatIdState] = useState<string | null>(null); // null = новый чат, он ещё не создан
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Актуальные значения для обработчиков событий и async-функций: их замыкания видят устаревший state.
  const modelIdRef = useRef<string | null>(null);
  const chatIdRef = useRef<string | null>(null);
  const sendingRef = useRef(false);
  const chatsCtl = useRef<AbortController | null>(null);
  const messagesCtl = useRef<AbortController | null>(null);

  const setModelId = useCallback((id: string | null) => {
    modelIdRef.current = id;
    setModelIdState(id);
  }, []);

  const setChatId = useCallback((id: string | null) => {
    chatIdRef.current = id;
    setChatIdState(id);
  }, []);

  const loadChats = useCallback(async (model: string) => {
    chatsCtl.current?.abort(); // старый ответ не должен перезаписать новый
    const ctl = new AbortController();
    chatsCtl.current = ctl;
    try {
      setChats(await chatsApi.getChatsByModel(model, ctl.signal));
    } catch (e) {
      if (!ctl.signal.aborted) setError(errorMessage(e));
    }
  }, []);

  const startNewChat = useCallback(() => {
    messagesCtl.current?.abort();
    setChatId(null);
    setMessages([]);
    setError(null);
  }, [setChatId]);

  const openChat = useCallback(
    async (id: string) => {
      messagesCtl.current?.abort();
      const ctl = new AbortController();
      messagesCtl.current = ctl;
      setChatId(id);
      setMessages([]);
      setError(null);
      try {
        setMessages(await chatsApi.getMessages(id, ctl.signal));
      } catch (e) {
        if (!ctl.signal.aborted) setError(errorMessage(e));
      }
    },
    [setChatId],
  );

  const selectModel = useCallback(
    (id: string) => {
      setModelId(id);
      setChats([]);
      startNewChat();
      void loadChats(id);
    },
    [setModelId, startNewChat, loadChats],
  );

  const refresh = useCallback(async () => {
    try {
      const list = await chatsApi.getModels();
      setModels(list);
      setError(null);

      const current = modelIdRef.current;
      const next = list.find((m) => m.id === current)?.id ?? list[0]?.id ?? null;
      if (next !== current) {
        setModelId(next);
        setChats([]);
        startNewChat();
      }
      if (next) await loadChats(next);
    } catch (e) {
      setError(errorMessage(e));
    }
  }, [setModelId, startNewChat, loadChats]);

  /** Удаляет чат. Если он открыт, переключает на «новый чат». Возвращает false, если удалить не удалось. */
  const removeChat = useCallback(
    async (id: string): Promise<boolean> => {
      if (sendingRef.current) return false;
      try {
        await chatsApi.deleteChat(id);
      } catch (e) {
        setError(errorMessage(e));
        const model = modelIdRef.current;
        if (model) void loadChats(model); // например, чат уже удалён в другой вкладке: список надо обновить
        return false;
      }
      setChats((prev) => prev.filter((chat) => chat.id !== id));
      if (chatIdRef.current === id) startNewChat();
      return true;
    },
    [loadChats, startNewChat],
  );

  /** Меняет текст сообщения открытого чата. Возвращает false, если не удалось (причина — в error). */
  const editMessage = useCallback(async (messageId: string, content: string): Promise<boolean> => {
    const chat = chatIdRef.current;
    if (!chat || sendingRef.current) return false;
    try {
      const updated = await chatsApi.editMessage(chat, messageId, content);
      setMessages((prev) => prev.map((m) => (m.id === messageId ? updated : m)));
      setError(null);
      return true;
    } catch (e) {
      setError(errorMessage(e));
      return false;
    }
  }, []);

  /** Удаляет сообщение открытого чата (остальные остаются). */
  const removeMessage = useCallback(
    async (messageId: string): Promise<boolean> => {
      const chat = chatIdRef.current;
      if (!chat || sendingRef.current) return false;
      try {
        await chatsApi.deleteMessage(chat, messageId);
      } catch (e) {
        setError(errorMessage(e));
        try {
          setMessages(await chatsApi.getMessages(chat)); // например, сообщение уже удалено в другой вкладке
        } catch {
          /* ошибка уже показана */
        }
        return false;
      }
      setMessages((prev) => prev.filter((m) => m.id !== messageId));
      setError(null);
      const model = modelIdRef.current;
      if (model) void loadChats(model); // обновить число сообщений в списке
      return true;
    },
    [loadChats],
  );

  /** Отправляет сообщение (с файлами или без); в «новом» чате сначала создаёт чат. Возвращает false, если отправить не удалось. */
  const send = useCallback(
    async (text: string, files: File[] = []): Promise<boolean> => {
      const model = modelIdRef.current;
      if (!model || sendingRef.current) return false;

      sendingRef.current = true;
      setSending(true);
      setError(null);

      let id = chatIdRef.current;
      const pendingId = `pending-${Date.now()}`;
      try {
        if (!id) {
          const chat = await chatsApi.createChat(model, titleFrom(text || files[0]?.name || "Файлы"));
          id = chat.id;
          setChatId(id);
        }

        // сообщение видно сразу, не дожидаясь ответа модели
        setMessages((prev) => [
          ...prev,
          {
            id: pendingId,
            role: "user",
            content: text,
            createdAt: new Date().toISOString(),
            attachments: files.map((f, i) => ({
              id: `${pendingId}-${i}`,
              fileName: f.name,
              contentType: f.type,
              fileSize: f.size,
            })),
          },
        ]);

        const result = await chatsApi.sendMessage(id, text, files);
        setMessages((prev) => [...prev.filter((m) => m.id !== pendingId), result.userMessage, result.assistantMessage]);
        return true;
      } catch (e) {
        setError(errorMessage(e));
        if (id) {
          // сервер мог сохранить сообщение до сбоя: показываем реальную историю вместо временной копии
          try {
            setMessages(await chatsApi.getMessages(id));
          } catch {
            /* ошибка уже показана */
          }
        }
        return false;
      } finally {
        sendingRef.current = false;
        setSending(false);
        void loadChats(model); // обновить порядок, время и число сообщений в списке
      }
    },
    [setChatId, loadChats],
  );

  useEffect(() => {
    if (active) void refresh();
  }, [active, refresh]);

  useEffect(
    () => () => {
      chatsCtl.current?.abort();
      messagesCtl.current?.abort();
    },
    [],
  );

  return { models, modelId, chats, chatId, messages, sending, error, selectModel, startNewChat, openChat, removeChat, editMessage, removeMessage, send };
}
