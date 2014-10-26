//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Anchura del dibujo de salida
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Altura del dibujo de salida
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Grosor de las articulaciones
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Grosor de la elipse central del cuerpo
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Grosor de los bordes rectangulares de la ventana
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Pincel usado para dibujar el punto central del cuerpo
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Pincel usado para dibujar artuculaciones rastreadas correctamente
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Pincel usado para dibujar artuculaciones no rastreadas correctamente
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen usado para dibujar huesos rastreados correctamente
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen usado para dibujar huesos no rastreados correctamente
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Sensor Kinect activo
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Grupo de dibujo para la salida del esqueleto renderizado
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Imagen que mostraremos por pantalla
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Inicializa una nueva instancia de la clase MainWindow (constructor)
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dibuja indicadores rectangulares para mostrar qué bordes están perdiendo información del esqueleto
        /// </summary>
        /// <param name="skeleton">esqueleto para el que se dibujan los bordes de pérdida de información</param>
        /// <param name="drawingContext">contexto de dibujo para el que dibujar</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            //Se comprueba si hay perdida de información en el borde inferior de la cámara
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                //En caso afirmativo, se dibuja un rectángulo rojo
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            //Se comprueba si hay perdida de información en el borde superior de la cámara
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            //Se comprueba si hay perdida de información en el borde izquierdo de la cámara
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }
            
            //Se comprueba si hay perdida de información en el borde derecho de la cámara
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Ejecuta tareas de inicio
        /// </summary>
        /// <param name="sender">objeto que envia el evento</param>
        /// <param name="e">argumentos del evento</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Crea el grupo de dibujo que se usará para dibujar durante la ejecución
            this.drawingGroup = new DrawingGroup();

            // Se define una fuente de imagen que podemos usar en el control de nuestra imagen
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Muestra por pantalla el dibujo usando nuestro control de imagen
            Image.Source = this.imageSource;

            // Mira a través de todos los sensores e inicia el primero conectado.
            // Esta acción requiere que un Kinect esté conectado cuando se ejecuta la aplicación.
            // Para hacer nuestra aplicación robusta con el plug/unplug,
            // se recomienda usar KinectSensorChooser disponible en Microsoft.Kinect.Toolkit (Véase componentes in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                //Si encuentra un sensor Kinect conectado lo asigna y sale del bucle.
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            //Si se ha encontrado un sensor conectado.
            if (null != this.sensor)
            {
                // Se activa el stream del esqueleto para recibir fotogramas del esqueleto.
                this.sensor.SkeletonStream.Enable();

                // Se añade un manejador de enventos para ser llamado siempre que haya datos de un nuevo fotograma de color.
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Se enciende el sensor Kinect.
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            //Si no se ha encontrado un sensor se muestra un mensaje de estado.
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Ejecuta tareas de apagado
        /// </summary>
        /// <param name="sender">objeto que envia el evento</param>
        /// <param name="e">argumentos del evento</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Si hay un sensor activo, se detiene.
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Manejador para los eventos SkeletonFrameReady de los sensores de Kinect
        /// </summary>
        /// <param name="sender">objeto que envia el evento</param>
        /// <param name="e">argumentos del evento</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Se declara un array de objetos esqueleto.
            Skeleton[] skeletons = new Skeleton[0];

            //Asigna una sintaxis determinada para un SkeletonFrame
            //http://msdn.microsoft.com/es-es/library/yh598w02.aspx
            //http://msdn.microsoft.com/es-es/library/zhdeatwt.aspx
            ///////////////////////////////////////////////////////
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Dibuja un fondo transparante para configurar el tamaño de renderizado.
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                //Si hay objetos en el array de skeleton creado anteriormente.
                if (skeletons.Length != 0)
                {
                    //Para cada uno de ellos.
                    foreach (Skeleton skel in skeletons)
                    {
                        //Se dibujan los borden en los que se pierde información.
                        RenderClippedEdges(skel, dc);

                        //Si el esqueleto está rastreado correctamente.
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            //Se dibuja el esqueleto.
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        //En otro caso, si solo se conoce la posición, se dibuja una elipse.
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // Nos evita dibujar fuera del area de renderizado
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Dibuja huesos y articulaciones del esqueleto
        /// </summary>
        /// <param name="skeleton">esqueleto a dibujar</param>
        /// <param name="drawingContext">contexto de dibujo para dibujar</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            if(checkMovTorsoPlanoXZ(45, ref skeleton, ref drawingContext)){
                // Renderizado del Torso
                this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
                this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
                this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
                this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);
            }

            // Brazo izquierdo
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Brazo derecho
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Pierna izquierda
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Pierna derecha
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Renderizado de articulaciones
            // Para cada articulacion, se elige un pincel con el que se va a pintar,
            // en función de si está rastreado correctamente o no.
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }



        /****************************************************************
         *                                                              
         *  Hasta aquí el fichero original de SkeletonBasics-WPF salvo
         *  la pequeña modificación el la función "DrawBonesAndJoints"
         *  en la que se llama a la función "checkMovTorsoPlanoXZ"
         *  definida a continuación) la cual devuelve si el movimiento
         *  en concreto es correcto o no.
         *  Si el movimiento es correcto, se devuelve 'true' y se 
         *  ejecutará el código dentro de la sentencia 'if' que hará
         *  que se pinte el torso de color verde tal y como se hace
         *  por defecto.
         *  Si el movimiento es incorrecto, se devuelve 'false' y es
         *  entonces cuando la función "checkMovTorsoPlanoXZ" se
         *  encargará de dibujar el torso de color rojo ya que las
         *  instrucciones dentro del 'if' no se ejecutarán.
         *  
         * **************************************************************/



        /// <summary>
        /// Duplicado de la función "Draw Bone" para dibujar los huesos en rojo.
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DibujarHuesoRojo(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                //Aquí es donde escogemos el pincel rojo en caso de que las dos articulaciones estén en estado 'Tracked'.
                //Si no es así, se dibujarán en gris, con el pincel definido por defecto 'inferredBonePen'.
                drawPen = new Pen(Brushes.Red, 1);
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Comprueba que el movimiento del torso en el plano XZ es correcto o no para un ángulo dado.
        /// </summary>
        /// <param name="angulo">angulo que al que se debe hacer el movimiento</param>
        /// <param name="skeleton">esqueleto del que se dibujan los huesos. Se pasa por referencia</param>
        /// <param name="drawingContext">contexto de dibujo. Se pasa por referencia</param>
        /// <returns name="true">Si el movimiento es correcto</returns>
        /// <returns name="false">Si el movimiento no es correcto</returns>

        private bool checkMovTorsoPlanoXZ(int angulo, ref Skeleton skeleton, ref DrawingContext drawingContext)
        {
            bool movimiento_correcto = false;

            //** http://msdn.microsoft.com/en-us/library/hh973073.aspx **//
            //Capturamos las coordenadas de los puntos que nos interesan para este movimiento.
            SkeletonPoint cadera = skeleton.Joints[JointType.HipCenter].Position;
            SkeletonPoint espalda = skeleton.Joints[JointType.Spine].Position;
            SkeletonPoint cuello = skeleton.Joints[JointType.ShoulderCenter].Position;

            /**************************************************************
             *    Cálculos para determinar si el movimiento es correcto.  *
             *                                                            *
             **************************************************************/

            //Identificación básica del movimiento. (Falta controlar el ángulo de inclinación.)
            if (cadera.Z > espalda.Z && espalda.Z > cuello.Z)
                movimiento_correcto = true;

            //Si el movimiento no es correcto se llama a la función de dibujado duplicada "DibujarHuesoRojo".
            if (!movimiento_correcto)
            {
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
                this.DibujarHuesoRojo(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);
            }
            
            return movimiento_correcto;
        }
    }
}

//Definimos un stream de escritura para la salida a un fichero de los datos captados.
/* if (frame % 10 == 0)
 {
     StreamWriter output_stream = new StreamWriter(@"C:\Users\Fran\Desktop\profundidad.txt", true);
     output_stream.WriteLine(frame);
     output_stream.WriteLine("cuello X:" + cuello.X + " Y:" + cuello.Y + " Z:" + cuello.Z);
     output_stream.WriteLine("espald X:" + espalda.X + " Y:" + espalda.Y + " Z:" + espalda.Z);
     output_stream.WriteLine("cadera X:" + cadera.X + " Y:" + cadera.Y + " Z:" + cadera.Z);
     output_stream.WriteLine("");
     output_stream.Close();
 }
 frame++;*/