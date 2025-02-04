import React, { useEffect, useRef, useState } from 'react';
import { Note, NoteStatus } from '../types/Note';
import './NoteEditor.css';  // Make sure this is the exact path

interface NoteEditorProps {
    note?: Note;
    onSave: (note: Partial<Note>) => void;
    onCancel: () => void;
}

interface HistoryEntry {
    content: string;
    cursorPosition: number;
}

const NoteEditor: React.FC<NoteEditorProps> = ({ note, onSave, onCancel }) => {
    console.log('NoteEditor rendered with note:', note);

    const [title, setTitle] = useState(note?.title || '');
    const [content, setContent] = useState(note?.content || '');
    const [category, setCategory] = useState(note?.category || '');
    const [isSaving, setIsSaving] = useState(false);
    const contentRef = useRef<HTMLTextAreaElement>(null);
    const [undoStack, setUndoStack] = useState<HistoryEntry[]>([]);
    const [redoStack, setRedoStack] = useState<HistoryEntry[]>([]);

    useEffect(() => {
        if (note) {
            setTitle(note.title);
            setContent(note.content);
            setCategory(note.category || '');
        } else {
            setTitle('');
            setContent('');
            setCategory('');
        }

        return () => {
            console.log('NoteEditor unmounting');
        };
    }, [note?.id]);

    const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const newContent = e.target.value;
        setContent(newContent);
        addToHistory(newContent);
    };

    const handleSave = async () => {
        try {
            setIsSaving(true);
            await onSave({
                ...(note || {}),
                title,
                content,
                category
            });
        } finally {
            setIsSaving(false);
        }
    };

    // Add undo/redo handling
    const addToHistory = (content: string) => {
        if (!contentRef.current) return;

        setUndoStack(prev => [...prev, {
            content,
            cursorPosition: contentRef.current?.selectionStart ?? 0
        }]);
        setRedoStack([]);
    };

    const undo = () => {
        if (undoStack.length === 0) return;

        const current = {
            content,
            cursorPosition: contentRef.current?.selectionStart ?? 0
        };
        const previous = undoStack[undoStack.length - 1];

        setRedoStack(prev => [...prev, current]);
        setUndoStack(prev => prev.slice(0, -1));
        setContent(previous.content);

        if (contentRef.current) {
            contentRef.current.selectionStart = previous.cursorPosition;
            contentRef.current.selectionEnd = previous.cursorPosition;
        }
    };

    const redo = () => {
        if (redoStack.length === 0) return;

        const current = {
            content,
            cursorPosition: contentRef.current?.selectionStart ?? 0
        };
        const next = redoStack[redoStack.length - 1];

        setUndoStack(prev => [...prev, current]);
        setRedoStack(prev => prev.slice(0, -1));
        setContent(next.content);

        if (contentRef.current) {
            contentRef.current.selectionStart = next.cursorPosition;
            contentRef.current.selectionEnd = next.cursorPosition;
        }
    };

    // Add keyboard shortcuts
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
                e.preventDefault();
                if (e.shiftKey) {
                    redo();
                } else {
                    undo();
                }
            }
        };

        document.addEventListener('keydown', handleKeyDown);
        return () => document.removeEventListener('keydown', handleKeyDown);
    }, [undo, redo]);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onSave({
            title,
            content,
            category,
            status: NoteStatus.Personal
        });
    };

    return (
        <form onSubmit={handleSubmit} className="note-editor">
            <input
                type="text"
                placeholder="Note Title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
            />
            <input
                type="text"
                placeholder="Category"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
            />
            <div className="editor-toolbar">
                <button type="button" onClick={undo} disabled={undoStack.length === 0}>
                    Undo
                </button>
                <button type="button" onClick={redo} disabled={redoStack.length === 0}>
                    Redo
                </button>
            </div>
            
            <textarea
                ref={contentRef}
                placeholder="Note Content"
                value={content}
                onChange={handleContentChange}
                required
            />
            <div className="button-group">
                <button type="submit" className="save-button" disabled={isSaving}>
                    {isSaving ? 'Saving...' : 'Save'}
                </button>
                <button type="button" onClick={onCancel} className="cancel-button">Cancel</button>
            </div>
        </form>
    );
};

export default NoteEditor;