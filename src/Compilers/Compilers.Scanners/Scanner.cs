﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VBF.Compilers.Scanners
{
    public class Scanner
    {
        //consts
        public const int InvalidTokenIndex = -1;
        public const int EndOfStreamTokenIndex = -100;

        private FiniteAutomationEngine m_engine;
        private SourceReader m_source;

        private int m_lastAcceptedTokenIndex;
        private SourceLocation m_lastTokenStart;
        private StringBuilder m_lexemeValueBuilder;

        private CacheQueue<Lexeme> m_lookAheadQueue;

        private int[] m_tokenAttributes;

        public Scanner(ScannerInfo scannerInfo)
        {

            m_engine = new FiniteAutomationEngine(scannerInfo);
            m_lexemeValueBuilder = new StringBuilder();
            m_tokenAttributes = new int[scannerInfo.TokenCount];

            Initialize();
        }

        private void Initialize()
        {
            m_engine.Reset();
            m_lastAcceptedTokenIndex = InvalidTokenIndex;
            m_lookAheadQueue = new CacheQueue<Lexeme>();
            m_lexemeValueBuilder.Clear();
        }

        public void SetSource(SourceReader source)
        {
            CodeContract.RequiresArgumentNotNull(source, "source");
            m_source = source;

            Initialize();
        }

        public int Peek()
        {
            return Peek(1);
        }

        public int Peek2()
        {
            return Peek(2);
        }

        public int Peek(int lookAhead)
        {
            CodeContract.RequiresArgumentInRange(lookAhead > 0, "lookAhead", "The lookAhead must be greater than zero");

            while (m_lookAheadQueue.Count < lookAhead)
            {
                //look ahead more
                m_lookAheadQueue.Enqueue(ReadNextToken());
            }
            Lexeme lookAheadLexeme = m_lookAheadQueue[lookAhead - 1];
            return lookAheadLexeme.TokenIdentityIndex;

        }

        public Lexeme Read()
        {
            if (m_lookAheadQueue.Count > 0)
            {
                return m_lookAheadQueue.Dequeue();
            }

            return ReadNextToken();
        }

        private Lexeme ReadNextToken()
        {
            //run to next stopped state
            m_engine.Reset();
            m_lastTokenStart = m_source.PeekLocation();
            m_lastAcceptedTokenIndex = InvalidTokenIndex;
            m_lexemeValueBuilder.Clear();

            if (m_source.PeekChar() < 0)
            {
                //return End Of Stream token
                return new Lexeme(EndOfStreamTokenIndex,
                    new SourceSpan(m_lastTokenStart, m_lastTokenStart), null);
            }

            int inputCharValue;
            while (true)
            {
                inputCharValue = m_source.PeekChar();

                if (inputCharValue < 0)
                {
                    //end of stream, treat as stopped
                    break;
                }

                char inputChar = (char)inputCharValue;
                m_engine.Input(inputChar);

                if (m_engine.IsAtAcceptState)
                {
                    m_lastAcceptedTokenIndex = m_engine.CurrentTokenIndex;
                }
                else if (m_engine.IsAtStoppedState)
                {
                    //stop immediately at a stopped state
                    break;
                }
                else
                {
                    m_lastAcceptedTokenIndex = InvalidTokenIndex;
                }

                m_source.ReadChar();
                m_lexemeValueBuilder.Append(inputChar);
            }

            return new Lexeme(m_lastAcceptedTokenIndex,
                new SourceSpan(m_lastTokenStart, m_source.Location), m_lexemeValueBuilder.ToString());
        }
    }
}
