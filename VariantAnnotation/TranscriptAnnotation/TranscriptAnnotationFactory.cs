﻿using System.Collections.Generic;
using Genome;
using Intervals;
using VariantAnnotation.AnnotatedPositions.Transcript;
using VariantAnnotation.Interface.AnnotatedPositions;
using VariantAnnotation.Interface.Caches;
using Variants;

namespace VariantAnnotation.TranscriptAnnotation
{
    public static class TranscriptAnnotationFactory
    {
        private static readonly AminoAcids AminoAcidsProvider     = new AminoAcids(false);
        private static readonly AminoAcids MitoAminoAcidsProvider = new AminoAcids(true);

        public static IList<IAnnotatedTranscript> GetAnnotatedTranscripts(IVariant variant,
            ITranscript[] transcriptCandidates, ISequence compressedSequence, IPredictionCache siftCache,
            IPredictionCache polyphenCache)
        {
            var annotatedTranscripts = new List<IAnnotatedTranscript>();

            foreach (var transcript in transcriptCandidates)
            {
                var annotationStatus = DecideAnnotationStatus(variant, transcript, variant.Behavior,
                    variant.Chromosome.FlankingLength);

                var annotatedTranscript = GetAnnotatedTranscript(variant, compressedSequence, transcript,
                    annotationStatus, siftCache, polyphenCache);

                if (annotatedTranscript != null) annotatedTranscripts.Add(annotatedTranscript);
            }

            return annotatedTranscripts;
        }

        private static IAnnotatedTranscript GetAnnotatedTranscript(IVariant variant, ISequence compressedSequence,
            ITranscript transcript, Status annotationStatus, IPredictionCache siftCache, IPredictionCache polyphenCache)
        {
            IAnnotatedTranscript annotatedTranscript = null;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (annotationStatus)
            {
                case Status.FlankingAnnotation:
                    annotatedTranscript = FlankingTranscriptAnnotator.GetAnnotatedTranscript(variant.End, transcript);
                    break;
                case Status.ReducedAnnotation:
                    annotatedTranscript = ReducedTranscriptAnnotator.GetAnnotatedTranscript(transcript, variant);
                    break;
                case Status.RohAnnotation:
                    annotatedTranscript = RohTranscriptAnnotator.GetAnnotatedTranscript(transcript);
                    break;
                case Status.FullAnnotation:
                    var acidsProvider = variant.Chromosome.UcscName == "chrM"
                        ? MitoAminoAcidsProvider
                        : AminoAcidsProvider;
                    annotatedTranscript = FullTranscriptAnnotator.GetAnnotatedTranscript(transcript, variant,
                        compressedSequence, siftCache, polyphenCache, acidsProvider);
                    break;
            }

            return annotatedTranscript;
        }

        internal static Status DecideAnnotationStatus(IInterval variant, IInterval transcript, AnnotationBehavior behavior, int flankingLength)
        {
            bool overlapsTranscript = variant.Overlaps(transcript);
            
            if (!behavior.ReducedTranscriptAnnotation)
            {
                // handle small variants
                if (overlapsTranscript) return Status.FullAnnotation;
                if (behavior.NeedFlankingTranscripts && variant.Overlaps(transcript, flankingLength)) return Status.FlankingAnnotation;
            }
            else if (overlapsTranscript)
            {
                // handle large variants
                if (behavior.CanonicalTranscriptOnly) return Status.RohAnnotation;
                return Status.ReducedAnnotation;
            }

            return Status.NoAnnotation;
        }

        public enum Status
        {
            NoAnnotation,
            FlankingAnnotation,
            ReducedAnnotation,
            FullAnnotation,
            RohAnnotation
        }
    }
}